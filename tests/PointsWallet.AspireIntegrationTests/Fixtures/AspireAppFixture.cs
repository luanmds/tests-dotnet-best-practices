using Aspire.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace PointsWallet.AspireIntegrationTests.Fixtures;

[CollectionDefinition(nameof(AspireAppCollection))]
public class AspireAppCollection : ICollectionFixture<AspireAppFixture>
{  
}

public class AspireAppFixture : IAsyncLifetime
{
    private IDistributedApplicationBuilder _appHostBuilder;
    private DistributedApplication _app;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    public static CancellationToken CancellationToken => new CancellationTokenSource(DefaultTimeout).Token;

    public HttpClient ApiClient { get; private set; }

    public async Task InitializeAsync()
    {
        _appHostBuilder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.PointsWallet_AppHost>([
                 "--environment=Testing",
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

        _appHostBuilder.Services.AddAuthentication(defaultScheme: TestAuthHandler.TestScheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.TestScheme, 
                    options => { });

        _app = _appHostBuilder.Build();

        await _app.StartAsync(CancellationToken)
            .WaitAsync(DefaultTimeout, CancellationToken);

        ApiClient = _app.CreateHttpClient("api");
        await _app.ResourceNotifications
            .WaitForResourceHealthyAsync("api", CancellationToken)
            .WaitAsync(DefaultTimeout, CancellationToken);

        // TODO: adjust seed data
        await SeedData();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();
 
    public async Task ExecuteDbQueryAsync(Func<NpgsqlConnection, Task> action)
    {
        var connectionString = await _app.GetConnectionStringAsync("postgres");

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(CancellationToken);
        await action(conn);
    }

    private async Task SeedData()
    {
        var seeds = new Seeds(this);
        await seeds.SeedNewUser();
        await seeds.SeedWalletForUser(Seeds.UserId);
    }
}
