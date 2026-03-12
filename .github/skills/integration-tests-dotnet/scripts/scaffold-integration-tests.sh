#!/usr/bin/env bash
# =============================================================================
# scaffold-integration-tests.sh
# =============================================================================
# Scaffolds a new integration test project for an ASP.NET Core API with:
#   - xUnit test framework
#   - Testcontainers.PostgreSql for database containers
#   - FluentAssertions for expressive assertions
#   - Microsoft.AspNetCore.Mvc.Testing for WebApplicationFactory
#   - Coverlet for code coverage
#
# Usage:
#   ./scaffold-integration-tests.sh <TestProjectName> <ApiProjectPath>
#
# Example:
#   ./scaffold-integration-tests.sh MyApp.IntegrationTests src/MyApp.Api/MyApp.Api.csproj
#
# The script will:
#   1. Create an xUnit test project in tests/<TestProjectName>/
#   2. Add required NuGet packages
#   3. Add a project reference to the API project
#   4. Scaffold boilerplate files (WebApplicationFactory, Fixture, TestAuthHandler)
#   5. Add the test project to the solution (if a .sln file is found)
# =============================================================================

set -euo pipefail

# --- Arguments ---------------------------------------------------------------
TEST_PROJECT_NAME="${1:?Usage: $0 <TestProjectName> <ApiProjectPath>}"
API_PROJECT_PATH="${2:?Usage: $0 <TestProjectName> <ApiProjectPath>}"

# --- Derived paths -----------------------------------------------------------
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
TEST_PROJECT_DIR="$REPO_ROOT/tests/$TEST_PROJECT_NAME"

echo "=== Scaffolding Integration Test Project ==="
echo "  Project:   $TEST_PROJECT_NAME"
echo "  API ref:   $API_PROJECT_PATH"
echo "  Location:  $TEST_PROJECT_DIR"
echo ""

# --- Step 1: Create xUnit project -------------------------------------------
if [ -d "$TEST_PROJECT_DIR" ]; then
    echo "⚠  Directory already exists: $TEST_PROJECT_DIR"
    echo "   Skipping project creation."
else
    echo "→ Creating xUnit project..."
    dotnet new xunit -n "$TEST_PROJECT_NAME" -o "$TEST_PROJECT_DIR" --framework net9.0
fi

# --- Step 2: Add NuGet packages ---------------------------------------------
echo ""
echo "→ Adding NuGet packages..."

cd "$TEST_PROJECT_DIR"

dotnet add package Testcontainers.PostgreSql
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package FluentAssertions
dotnet add package coverlet.collector

echo ""
echo "→ Adding project reference to API..."
dotnet add reference "$REPO_ROOT/$API_PROJECT_PATH"

# --- Step 3: Add global usings ----------------------------------------------
echo ""
echo "→ Configuring global usings..."

# Ensure Xunit global using is in the csproj
if ! grep -q '<Using Include="Xunit"' "$TEST_PROJECT_DIR/$TEST_PROJECT_NAME.csproj"; then
    sed -i '/<\/Project>/i \
  <ItemGroup>\
    <Using Include="Xunit" />\
  </ItemGroup>' "$TEST_PROJECT_DIR/$TEST_PROJECT_NAME.csproj"
fi

# --- Step 4: Scaffold boilerplate files --------------------------------------
echo ""
echo "→ Scaffolding boilerplate files..."

# Derive namespace from project name (replace dots with dots, it's already valid)
NAMESPACE="$TEST_PROJECT_NAME"
# Extract DbContext name hint from API project name
API_NAME=$(basename "$API_PROJECT_PATH" .csproj)

mkdir -p "$TEST_PROJECT_DIR/Fixtures"
mkdir -p "$TEST_PROJECT_DIR/Api"

# --- WebApplicationFactory ---
if [ ! -f "$TEST_PROJECT_DIR/CustomWebApplicationFactory.cs" ]; then
cat > "$TEST_PROJECT_DIR/CustomWebApplicationFactory.cs" << 'FACTORY_EOF'
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NAMESPACE_PLACEHOLDER;

// TODO: Replace YourDbContext with your actual DbContext class
internal class CustomWebApplicationFactory(string connectionString)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // TODO: Replace DbContextOptions<YourDbContext> with your DbContext
            // services.RemoveAll<DbContextOptions<YourDbContext>>();
            // services.AddDbContext<YourDbContext>(options =>
            //     options.UseNpgsql(connectionString));

            services.AddAuthentication(defaultScheme: TestAuthHandler.TestScheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.TestScheme,
                    options => { });
        });

        builder.UseEnvironment("Testing");
    }
}
FACTORY_EOF
    sed -i "s/NAMESPACE_PLACEHOLDER/$NAMESPACE/" "$TEST_PROJECT_DIR/CustomWebApplicationFactory.cs"
    echo "   Created: CustomWebApplicationFactory.cs"
fi

# --- Shared Fixture ---
if [ ! -f "$TEST_PROJECT_DIR/Fixtures/WebApplicationFixture.cs" ]; then
cat > "$TEST_PROJECT_DIR/Fixtures/WebApplicationFixture.cs" << 'FIXTURE_EOF'
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace NAMESPACE_PLACEHOLDER.Fixtures;

public class WebApplicationFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private CustomWebApplicationFactory _application = null!;

    public HttpClient Client { get; private set; } = null!;

    public WebApplicationFixture()
    {
        _container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("testdb")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var connectionString = _container.GetConnectionString();
        _application = new CustomWebApplicationFactory(connectionString);

        // TODO: Apply EF Core migrations
        // using var scope = _application.Services.CreateScope();
        // var dbContext = scope.ServiceProvider.GetRequiredService<YourDbContext>();
        // await dbContext.Database.MigrateAsync();

        Client = _application.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        _application?.Dispose();
        await _container.DisposeAsync();
    }
}
FIXTURE_EOF
    sed -i "s/NAMESPACE_PLACEHOLDER/$NAMESPACE/" "$TEST_PROJECT_DIR/Fixtures/WebApplicationFixture.cs"
    echo "   Created: Fixtures/WebApplicationFixture.cs"
fi

# --- TestAuthHandler ---
if [ ! -f "$TEST_PROJECT_DIR/Fixtures/TestAuthHandler.cs" ]; then
cat > "$TEST_PROJECT_DIR/Fixtures/TestAuthHandler.cs" << 'AUTH_EOF'
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
AUTH_EOF
    echo "   Created: Fixtures/TestAuthHandler.cs"
fi

# --- Step 5: Add to solution ------------------------------------------------
echo ""
SLN_FILE=$(find "$REPO_ROOT" -maxdepth 1 -name "*.sln" -print -quit)
if [ -n "$SLN_FILE" ]; then
    echo "→ Adding to solution: $(basename "$SLN_FILE")"
    dotnet sln "$SLN_FILE" add "$TEST_PROJECT_DIR/$TEST_PROJECT_NAME.csproj" \
        --solution-folder tests 2>/dev/null || true
else
    echo "⚠  No .sln file found in repo root. Add the project manually."
fi

# --- Done --------------------------------------------------------------------
echo ""
echo "=== Scaffolding Complete ==="
echo ""
echo "Next steps:"
echo "  1. Update CustomWebApplicationFactory.cs with your DbContext"
echo "  2. Update Fixtures/WebApplicationFixture.cs to apply migrations"
echo "  3. Create test classes in the Api/ directory"
echo "  4. Run tests: dotnet test $TEST_PROJECT_DIR"
echo ""
