---
name: integration-tests-dotnet
description: Write integration tests for ASP.NET Core APIs
argument-hint: "[endpoint or scenario to test]"
---

# Integration Tests for .NET with xUnit & Testcontainers

A comprehensive guide for writing integration tests for ASP.NET Core APIs backed by
PostgreSQL using real Docker containers. Tests run against the full application pipeline
(Minimal API → MediatR/CQRS → EF Core → PostgreSQL) with zero mocking of infrastructure.

## When to Use This Skill

Use this skill when you need to:

- Test API endpoints end-to-end against a real database
- Verify EF Core repository operations with actual PostgreSQL
- Validate the full MediatR pipeline (validators, behaviors, handlers)
- Test authentication/authorization flows with a controlled identity
- Create new integration test projects or add tests to existing ones
- Extend test infrastructure with additional containerized services

**Do NOT use** for unit tests (aggregate logic, value objects, validators in isolation).
For those, use standard xUnit + NSubstitute/Moq without containers.

## Prerequisites

- **Docker** installed and running (Docker Desktop or Docker Engine)
- **.NET 9.0+** (C# 13)
- **Test framework**: xUnit 2.9+
- **Required NuGet packages**:
  - `Testcontainers.PostgreSql` — PostgreSQL container module
  - `Microsoft.AspNetCore.Mvc.Testing` — `WebApplicationFactory<Program>`
  - `FluentAssertions` — Expressive assertions
  - `Microsoft.NET.Test.Sdk` — Test SDK
  - `coverlet.collector` — Code coverage

To scaffold a new integration test project, use the [scaffold script](scripts/scaffold-integration-tests.sh).

## Architecture Overview

The integration test infrastructure has four components that work together:

```
┌─────────────────────────────────────────────────────────┐
│  Test Class (IClassFixture<Fixture>)                    │
│  - Uses HttpClient from fixture                         │
│  - Sends requests to API endpoints                      │
│  - Asserts responses with FluentAssertions              │
├─────────────────────────────────────────────────────────┤
│  Shared Fixture (IAsyncLifetime)                        │
│  - Starts PostgreSqlContainer                           │
│  - Creates WebApplicationFactory with connection string │
│  - Applies EF Core migrations                           │
│  - Exposes pre-configured HttpClient                    │
├─────────────────────────────────────────────────────────┤
│  WebApplicationFactory<Program>                         │
│  - Replaces DbContextOptions with test container DB     │
│  - Replaces auth with TestAuthHandler                   │
│  - Sets environment to "Testing"                        │
├─────────────────────────────────────────────────────────┤
│  TestAuthHandler                                        │
│  - Always returns AuthenticateResult.Success            │
│  - Provides deterministic test claims                   │
└─────────────────────────────────────────────────────────┘
```

## Instructions

### Step-by-Step: Creating a New Integration Test

Follow these steps when creating integration tests for an endpoint:

1. **Identify the endpoint** — Determine the HTTP method, route, request/response DTOs
2. **Check if a shared fixture exists** — Reuse `IClassFixture<>` to avoid spinning up new containers
3. **Create the test class** — Use primary constructor with `IClassFixture<Fixture>` injection
4. **Write test methods** — Follow `MethodName_Scenario_ExpectedResult` naming convention
5. **Use `PostAsJsonAsync`/`GetAsync`** — Send HTTP requests through the factory's `HttpClient`
6. **Assert with FluentAssertions** — Deserialize responses and use `.Should()` chains
7. **Run tests** — Execute `dotnet test` (Docker must be running)

### Core Pattern 1: WebApplicationFactory

The custom `WebApplicationFactory<Program>` replaces real infrastructure with test infrastructure.
It accepts a connection string from the Testcontainers PostgreSQL container and swaps the
real `DbContextOptions` and authentication scheme.

```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

internal class PointsWalletWebApplicationFactory(string connectionString)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove the real database registration
            services.RemoveAll<DbContextOptions<PointsWalletDbContext>>();

            // Register the test container database
            services.AddDbContext<PointsWalletDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Replace authentication with a handler that always succeeds
            services.AddAuthentication(defaultScheme: TestAuthHandler.TestScheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.TestScheme,
                    options => { });
        });

        builder.UseEnvironment("Testing");
    }
}
```

**Key points**:
- Use **primary constructor** to accept the connection string from the fixture
- `RemoveAll<DbContextOptions<TContext>>()` ensures the real DB registration is fully replaced
- `ConfigureTestServices` runs **after** `Program.cs` service registration, so it overrides production services
- Set a dedicated environment (`"Testing"`) to disable middleware like auto-migration in development

See full example: [WebApplicationFactory.example.cs](examples/WebApplicationFactory.example.cs)

### Core Pattern 2: Shared Container Fixture

The fixture manages the PostgreSQL container lifecycle and provides an `HttpClient` to test classes.
It implements `IAsyncLifetime` for async setup/teardown and is shared via `IClassFixture<>`.

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

public class PointsWalletWebApplicationFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private PointsWalletWebApplicationFactory _application = null!;

    public HttpClient Client { get; private set; } = null!;

    public PointsWalletWebApplicationFixture()
    {
        _container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("pointswalletdb_test")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var connectionString = _container.GetConnectionString();
        _application = new PointsWalletWebApplicationFactory(connectionString);

        using var scope = _application.Services.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<PointsWalletDbContext>();
        await dbContext.Database.MigrateAsync();

        Client = _application.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        _application?.Dispose();
        await _container.DisposeAsync();
    }
}
```

**Key points**:
- Pin the Docker image version (`postgres:17-alpine`) for CI reproducibility
- Apply EF Core migrations (`MigrateAsync()`) so the schema is ready before tests run
- The container is started **once** per test class (via `IClassFixture<>`) — not per test method
- Dispose in reverse order: `HttpClient` → `WebApplicationFactory` → container

See full example: [Fixture.example.cs](examples/Fixture.example.cs)

### Core Pattern 3: Test Authentication Handler

The `TestAuthHandler` bypasses real authentication so integration tests can call
endpoints that require authorization without needing real JWT tokens.

```csharp
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string TestScheme = "TestScheme";
    public const string TestUserId = "test-user-id";
    public const string TestUserEmail = "test@example.com";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId),
            new Claim(ClaimTypes.Email, TestUserEmail),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim("sub", TestUserId)
        };

        var identity = new ClaimsIdentity(claims, TestScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, TestScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

**Key points**:
- Uses **primary constructor** for DI parameters
- Always returns `AuthenticateResult.Success` — every request is authenticated
- Provides deterministic claims (`TestUserId`, `TestUserEmail`) that tests can reference
- Registered in `WebApplicationFactory.ConfigureTestServices` replacing the real auth scheme

See full example: [TestAuthHandler.example.cs](examples/TestAuthHandler.example.cs)

### Core Pattern 4: Endpoint Integration Tests

Test classes use `IClassFixture<>` with the shared fixture and receive a pre-configured
`HttpClient` through the primary constructor.

```csharp
using System.Net.Http.Json;
using FluentAssertions;

public class UserEndpointsTests(PointsWalletWebApplicationFixture fixture)
    : IClassFixture<PointsWalletWebApplicationFixture>
{
    private readonly HttpClient _client = fixture.Client;

    [Fact]
    public async Task CreateUser_WithValidRequest_ReturnsCreatedWithUserId()
    {
        var request = new CreateUserRequest("John Doe", "john.doe@example.com");

        var response = await _client.PostAsJsonAsync("/api/users/", request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var content = await response.Content
            .ReadFromJsonAsync<CreateUserResponse>();
        content.Should().NotBeNull();
        content!.UserId.Should().NotBeNullOrEmpty();
    }
}
```

```csharp
public class WalletEndpointsTests(PointsWalletWebApplicationFixture fixture)
    : IClassFixture<PointsWalletWebApplicationFixture>
{
    private readonly HttpClient _client = fixture.Client;

    [Theory]
    [InlineData("My Wallet")]
    [InlineData(null)]
    public async Task CreateWallet_WithValidRequest_ReturnsCreatedWithWalletId(
        string? symbolicName)
    {
        var userId = Guid.NewGuid().ToString();
        var request = new CreateWalletRequest(symbolicName);

        var response = await _client.PostAsJsonAsync(
            $"/api/users/{userId}/wallets", request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var content = await response.Content
            .ReadFromJsonAsync<CreateWalletResponse>();
        content.Should().NotBeNull();
        content!.WalletId.Should().NotBeNullOrEmpty();
    }
}
```

See full example: [EndpointTests.example.cs](examples/EndpointTests.example.cs)

## Test Conventions

### Naming Pattern (Mandatory)

Always use: **`MethodName_Scenario_ExpectedResult`**

```csharp
// ✅ Correct
CreateUser_WithValidRequest_ReturnsCreatedWithUserId()
CreateWallet_WithSymbolicName_ReturnsCreatedWithWalletId()
GetUser_WithNonExistentId_ReturnsNotFound()

// ❌ Wrong
CreateUserRequest_ShouldCallCommandAndReturnSuccess()
TestCreateUser()
Test1()
```

### Structure Rules

- Follow **AAA pattern** (Arrange, Act, Assert) — but **NEVER emit AAA comments**
  - The structure should be self-evident from blank lines separating the three sections
- Use **FluentAssertions** for all assertions:
  ```csharp
  response.StatusCode.Should().Be(HttpStatusCode.Created);
  content.Should().NotBeNull();
  content!.UserId.Should().NotBeNullOrEmpty();
  ```
- **One logical assertion group per test** — split unrelated assertions into separate tests
- Use **`[Fact]`** for single-case tests, **`[Theory]` + `[InlineData]`** for parameterized tests
- Use **primary constructors** for test class DI (fixture injection)

### File Organization

```
tests/
  ProjectName.IntegrationTests/
    ProjectName.IntegrationTests.csproj
    CustomWebApplicationFactory.cs        # WebApplicationFactory override
    Fixtures/
      WebApplicationFixture.cs            # Shared container fixture
      TestAuthHandler.cs                  # Auth bypass handler
    Api/
      Users/
        UserEndpointsTests.cs             # Tests grouped by endpoint/feature
      Wallets/
        WalletEndpointsTests.cs
```

## Extending to Other Services

The Testcontainers ecosystem provides **65+ pre-configured modules**. You can extend
the fixture to include additional services by adding more containers.

### Adding Redis

```bash
dotnet add package Testcontainers.Redis
dotnet add package StackExchange.Redis
```

```csharp
public class AppFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;
    private readonly RedisContainer _redis;

    public AppFixture()
    {
        _postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("testdb")
            .Build();

        _redis = new RedisBuilder("redis:7-alpine").Build();
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _postgres.StartAsync(),
            _redis.StartAsync());

        // Pass both connection strings to the factory
    }

    public async Task DisposeAsync()
    {
        await _redis.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
```

Override additional services in `WebApplicationFactory.ConfigureTestServices`:

```csharp
builder.ConfigureTestServices(services =>
{
    // Replace Redis connection
    services.RemoveAll<IConnectionMultiplexer>();
    services.AddSingleton<IConnectionMultiplexer>(
        ConnectionMultiplexer.Connect(redisConnectionString));
});
```

### Adding RabbitMQ

```bash
dotnet add package Testcontainers.RabbitMq
dotnet add package RabbitMQ.Client
```

```csharp
private readonly RabbitMqContainer _rabbitmq =
    new RabbitMqBuilder("rabbitmq:3-management-alpine").Build();

// Use _rabbitmq.GetConnectionString() for connection factory
```

### Module Discovery

- Browse all modules: https://testcontainers.com/modules/?language=dotnet
- NuGet pattern: `Testcontainers.<ServiceName>`
- GitHub: https://github.com/testcontainers/testcontainers-dotnet

## Best Practices

### Container Management
- **Always use pre-configured modules** over generic `ContainerBuilder` when available
- **Pin Docker image versions** (e.g., `postgres:17-alpine`, not `postgres:latest`) for CI stability
- **Use random host ports** — never use `WithPortBinding(5432, 5432)` fixed bindings
- **Share containers via `IClassFixture<>`** — one container per test class, not per test method
- **Start containers in parallel** with `Task.WhenAll()` when the fixture uses multiple services
- **Never use `Task.Delay()` or `Thread.Sleep()`** as a container readiness mechanism

### Test Reliability
- **Apply EF Core migrations** in fixture's `InitializeAsync` before creating the `HttpClient`
- **Dispose in reverse order**: HttpClient → WebApplicationFactory → containers
- **Use `IAsyncLifetime`** — not constructors — for container lifecycle management  
- **Let Ryuk handle cleanup** on crashes, but always implement explicit `DisposeAsync`

### Code Quality
- **No AAA comments** — structure should be self-evident
- **`MethodName_Scenario_ExpectedResult`** naming — no exceptions
- **FluentAssertions only** — no raw `Assert.*` calls
- **Primary constructors** for fixture injection and DI parameters
- **One logical assertion group per test** — split unrelated concerns into separate tests
- **Use `ReadFromJsonAsync<T>`** for response deserialization, not manual JSON parsing

### CI/CD
- Ensure Docker is available in CI (GitHub Actions: use `ubuntu-latest` runner)
- Set reasonable timeouts for container startup in slow CI environments
- Use `coverlet.collector` for code coverage with `--collect:"XPlat Code Coverage"`
- Run with the [run-tests script](scripts/run-tests.sh) for consistent execution

## Troubleshooting

### Docker not available

```
Docker is not available. Please start Docker or install it.
```

Verify Docker is running: `docker info`. On CI, ensure the runner has Docker access.

### Container startup timeout

Increase timeout or check image availability:

```bash
# Pull the image manually to verify
docker pull postgres:17-alpine

# Check for leaked containers
docker ps -a | grep testcontainers
```

### Port conflicts

Testcontainers uses random ports by default. If you see port conflicts:
- Do NOT use fixed port bindings like `.WithPortBinding(5432, 5432)`
- Check for leaked containers: `docker ps -a`

### Ryuk cleanup issues

Ryuk (testcontainers/ryuk) is a sidecar container that auto-cleans test containers.
If containers are not being cleaned up:

```bash
# Verify Ryuk is running
docker ps | grep ryuk

# Force cleanup
docker rm -f $(docker ps -aq --filter "label=org.testcontainers=true")
```

### EF Core migration errors

If migrations fail during `InitializeAsync`, ensure:
- The connection string from `_container.GetConnectionString()` is correct
- The container has fully started before calling `MigrateAsync()`
- Migrations are up to date (`dotnet ef migrations list`)

## Additional Resources

- **Testcontainers .NET docs**: https://dotnet.testcontainers.org/
- **Testcontainers modules**: https://testcontainers.com/modules/?language=dotnet
- **NuGet packages**: https://www.nuget.org/packages?q=testcontainers
- **GitHub repository**: https://github.com/testcontainers/testcontainers-dotnet
- **WebApplicationFactory docs**: https://learn.microsoft.com/aspnet/core/test/integration-tests
- **FluentAssertions docs**: https://fluentassertions.com/
- **xUnit docs**: https://xunit.net/
