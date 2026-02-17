using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PointsWallet.Infrastructure;

namespace PointsWallet.IntegrationTests;

internal class PointsWalletWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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

    private static PointsWalletDbContext CreateDbContext(IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PointsWalletDbContext>();

        return dbContext;
    }
}
