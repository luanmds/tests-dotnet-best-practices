using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PointsWallet.Infrastructure;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace PointsWallet.IntegrationTests.Fixtures;

public class PointsWalletWebApplicationFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;
    private readonly RabbitMqContainer _rabbitMqContainer;
    private PointsWalletWebApplicationFactory _application = null!;

    public HttpClient Client { get; private set; } = null!;

    public PointsWalletWebApplicationFixture()
    {
        _dbContainer = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("pointswalletdb_test")
            .Build();
        
        _rabbitMqContainer = new RabbitMqBuilder("rabbitmq:4-management-alpine")
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();
        
        var connectionString = _dbContainer.GetConnectionString();
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
        await _dbContainer.DisposeAsync();
    }

    public async Task ExecuteDbContextAsync(Func<PointsWalletDbContext, Task> action)
    {
        using var scope = _application.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PointsWalletDbContext>();
        await action(dbContext);
    }
}
