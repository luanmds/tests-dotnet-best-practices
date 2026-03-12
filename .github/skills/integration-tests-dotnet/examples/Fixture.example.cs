// =============================================================================
// Shared Container Fixture — Manages PostgreSQL Testcontainer lifecycle
// =============================================================================
// This fixture is shared across all test methods in a test class via
// IClassFixture<PointsWalletWebApplicationFixture>. It:
//
//   1. Starts a real PostgreSQL container (postgres:17-alpine)
//   2. Creates a WebApplicationFactory connected to the container
//   3. Applies EF Core migrations so the schema is ready
//   4. Exposes a pre-configured HttpClient for sending HTTP requests
//
// The container is started ONCE per test class (not per test method),
// providing a good balance between isolation and performance.
// =============================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PointsWallet.Infrastructure;
using Testcontainers.PostgreSql;

namespace PointsWallet.IntegrationTests.Fixtures;

public class PointsWalletWebApplicationFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private PointsWalletWebApplicationFactory _application = null!;

    public HttpClient Client { get; private set; } = null!;

    public PointsWalletWebApplicationFixture()
    {
        // Pin the image version for reproducible builds in CI.
        // Use Alpine variants for smaller image size and faster pulls.
        _container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("pointswalletdb_test")
            .Build();
    }

    public async Task InitializeAsync()
    {
        // Start the PostgreSQL container — this pulls the image if needed
        await _container.StartAsync();

        // Get the dynamically assigned connection string (random host port)
        var connectionString = _container.GetConnectionString();

        // Create the custom WebApplicationFactory with the test DB
        _application = new PointsWalletWebApplicationFactory(connectionString);

        // Apply EF Core migrations before any test runs.
        // This ensures the database schema matches the application's model.
        using var scope = _application.Services.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<PointsWalletDbContext>();
        await dbContext.Database.MigrateAsync();

        // Create the HttpClient that tests will use to call API endpoints.
        // This client is configured to route requests to the in-memory test server.
        Client = _application.CreateClient();
    }

    public async Task DisposeAsync()
    {
        // Dispose in reverse order of creation:
        // 1. HttpClient  2. WebApplicationFactory  3. Container
        Client?.Dispose();
        _application?.Dispose();
        await _container.DisposeAsync();
    }
}
