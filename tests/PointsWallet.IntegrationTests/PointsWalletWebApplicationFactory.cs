using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PointsWallet.Infrastructure;
using PointsWallet.IntegrationTests.Fixtures;

namespace PointsWallet.IntegrationTests;

internal class PointsWalletApiWebApplicationFactory(
    string connectionString,
    string rabbitMqConnectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var rabbitUri = new Uri(rabbitMqConnectionString);

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            var testSettings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["RabbitMQ:Host"] = rabbitUri.Host,
                ["RabbitMQ:Port"] = rabbitUri.Port.ToString(),
                ["RabbitMQ:Username"] = PointsWalletWebApplicationFixture.RabbitMqUsername,
                ["RabbitMQ:Password"] = PointsWalletWebApplicationFixture.RabbitMqPassword
            };

            configurationBuilder.AddInMemoryCollection(testSettings);
        });

        builder.ConfigureLogging(logging =>
        {
            logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<PointsWalletDbContext>>();

            services.AddDbContext<PointsWalletDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddAuthentication(defaultScheme: TestAuthHandler.TestScheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.TestScheme, 
                    options => { });
        });
        
        builder.UseEnvironment("Testing");
    }
}
