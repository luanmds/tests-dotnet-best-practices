using System.Net.Http.Headers;
using Aspire.Hosting;
using Microsoft.Extensions.Logging;
using PointsWallet.Infrastructure.Messaging;

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
            // Bypass dev certificate validation so tests can use HTTPS directly
            // without the HTTP->HTTPS redirect (which drops the Authorization header)
            clientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });
            clientBuilder.AddStandardResilienceHandler();
        });

        _appHostBuilder.Services.AddMessaging(_appHostBuilder.Configuration);

        _app = _appHostBuilder.Build();
        
        await _app.StartAsync(CancellationToken)
            .WaitAsync(DefaultTimeout, CancellationToken);

        ApiClient = _app.CreateHttpClient("api", "https");
        ApiClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenFactory.GenerateToken());

        await _app.ResourceNotifications
            .WaitForResourceHealthyAsync("api", CancellationToken)
            .WaitAsync(DefaultTimeout, CancellationToken);

    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

}
