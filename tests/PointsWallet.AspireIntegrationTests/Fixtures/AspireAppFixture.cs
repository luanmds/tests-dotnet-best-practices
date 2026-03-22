using System.Net.Http.Headers;
using Aspire.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using PointsWallet.Infrastructure;

namespace PointsWallet.AspireIntegrationTests.Fixtures;

[CollectionDefinition(nameof(AspireAppCollection))]
public class AspireAppCollection : ICollectionFixture<AspireAppFixture>
{  
}

public class AspireAppFixture : IAsyncLifetime
{
    private IDistributedApplicationBuilder _appHostBuilder = null!;
    private DistributedApplication _app = null!;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    public static CancellationToken CancellationToken => new CancellationTokenSource(DefaultTimeout).Token;

    public HttpClient ApiClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        
        _appHostBuilder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.PointsWallet_AppHost>([
                 "UseVolumes=false"
            ]);

        _appHostBuilder.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddFilter(_appHostBuilder.Environment.ApplicationName, LogLevel.Information);
            logging.AddFilter("Aspire.", LogLevel.Information);
        });
        
        _appHostBuilder.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        _appHostBuilder.Services
            .AddAuthentication(defaultScheme: TestAuthHandler.TestScheme)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.TestScheme, 
                options => { });

        _app = _appHostBuilder.Build();

        await _app.StartAsync(CancellationToken)
            .WaitAsync(DefaultTimeout, CancellationToken);

        ApiClient = _app.CreateHttpClient("api");
        ApiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.TestScheme);
        await _app.ResourceNotifications
            .WaitForResourceHealthyAsync("api", CancellationToken)
            .WaitAsync(DefaultTimeout, CancellationToken);

        // TODO: migrations not running on startup, investigate
        // await RunMigrationsAsync();
        // await SeedData();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();
 
    public async Task ExecuteDbQueryAsync(Func<NpgsqlConnection, Task> action)
    {
        var connectionString = await _app.GetConnectionStringAsync("pointswalletdb");

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(CancellationToken);
        await action(conn);
    }

    private async Task RunMigrationsAsync()
    {
        var connectionString = await _app.GetConnectionStringAsync("pointswalletdb");

        var options = new DbContextOptionsBuilder<PointsWalletDbContext>()
            .UseNpgsql(connectionString, b => b.MigrationsAssembly(typeof(PointsWalletDbContext).Assembly.FullName))
            .Options;

        using var context = new PointsWalletDbContext(options);
        await context.Database.MigrateAsync(CancellationToken);
    }

    private async Task SeedData()
    {
        var seeds = new Seeds(this);
        await seeds.SeedNewUser();
        await seeds.SeedWalletForUser(Seeds.UserId);
    }
}
