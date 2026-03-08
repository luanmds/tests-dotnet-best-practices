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
        _container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("pointswalletdb_test")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        
        var connectionString = _container.GetConnectionString();
        _application = new PointsWalletWebApplicationFactory(connectionString);
        
        // Apply migrations before creating the client
        using var scope = _application.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PointsWalletDbContext>();
        await dbContext.Database.MigrateAsync();
        
        Client = _application.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        _application?.Dispose();
        await _container.DisposeAsync();
    }

    public async Task ExecuteDbContextAsync(Func<PointsWalletDbContext, Task> action)
    {
        using var scope = _application.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PointsWalletDbContext>();
        await action(dbContext);
    }
}
