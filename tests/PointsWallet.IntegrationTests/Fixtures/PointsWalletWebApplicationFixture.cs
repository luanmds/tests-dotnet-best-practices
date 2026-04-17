using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PointsWallet.Contracts.Messages;
using PointsWallet.Domain;
using PointsWallet.Domain.Behaviors;
using PointsWallet.Domain.Commands.AddPoints;
using PointsWallet.Infrastructure;
using PointsWallet.Infrastructure.Messaging;
using PointsWallet.Worker.Messaging;
using PointsWallet.Worker.Messaging.Mappers;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace PointsWallet.IntegrationTests.Fixtures;

public class PointsWalletWebApplicationFixture : IAsyncLifetime
{

    public const string RabbitMqUsername = "guest";
    public const string RabbitMqPassword = "guest";

    private readonly PostgreSqlContainer _dbContainer;
    private readonly RabbitMqContainer _rabbitMqContainer;
    private PointsWalletApiWebApplicationFactory _application = null!;
    private IHost _workerHost = null!;

    public HttpClient Client { get; private set; } = null!;
    public IServiceProvider Services => _application.Services;

    public PointsWalletWebApplicationFixture()
    {
        _dbContainer = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("pointswalletdb_test")
            .Build();
        
        _rabbitMqContainer = new RabbitMqBuilder("rabbitmq:4-management-alpine")
            .WithUsername(RabbitMqUsername)
            .WithPassword(RabbitMqPassword)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();
        
        var connectionString = _dbContainer.GetConnectionString();
        var rabbitMqConnectionString = _rabbitMqContainer.GetConnectionString();
        _application = new PointsWalletApiWebApplicationFactory(connectionString, rabbitMqConnectionString);
        
        // Apply migrations before creating the client
        using var scope = _application.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PointsWalletDbContext>();
        await dbContext.Database.MigrateAsync();

        // Start the worker host with the same configuration as the API
        _workerHost = CreateWorkerHost(connectionString, rabbitMqConnectionString);
        await _workerHost.StartAsync();

        // Seed initial data
        await SeedData();
        
        Client = _application.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (_workerHost is not null)
        {
            await _workerHost.StopAsync();
            _workerHost.Dispose();
        }

        _application?.Dispose();
        await _rabbitMqContainer.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    public async Task ExecuteDbContextAsync(Func<PointsWalletDbContext, Task> action)
    {
        using var scope = _application.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PointsWalletDbContext>();
        await action(dbContext);
    }

    private static IHost CreateWorkerHost(string connectionString, string rabbitMqConnectionString)
    {
        var workerSettings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:pointswalletdb"] = connectionString,
            ["ConnectionStrings:rabbitmq"] = rabbitMqConnectionString
        };

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(workerSettings);

        builder.Services.AddDomain();
        builder.Services.AddInfrastructure(builder.Configuration);

        builder.Services
            .AddMessageConsumer()
            .HandleMessage<AddPointsMessage, AddPointsMessageMapper>();

        builder.Services.AddMessaging(builder.Configuration, busConfigurator =>
        {
            busConfigurator.AddConsumer<MessageConsumer>();
        });

        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<AddPointsCommand>();
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
        
        return builder.Build();
    }

    private async Task SeedData()
    {
        var seeds = new Seeds(this);
        await seeds.SeedNewUser();
        await seeds.SeedWalletForUser(Seeds.UserId);
    }
}
