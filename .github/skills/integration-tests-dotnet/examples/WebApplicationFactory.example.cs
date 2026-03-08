// =============================================================================
// WebApplicationFactory Pattern — Custom factory for integration tests
// =============================================================================
// This factory inherits from WebApplicationFactory<Program> and replaces
// production services (database, authentication) with test-specific ones.
//
// The connection string is injected via primary constructor from the shared
// fixture, which gets it from the Testcontainers PostgreSQL container.
// =============================================================================

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PointsWallet.Infrastructure;

namespace PointsWallet.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory that replaces real infrastructure services
/// with test container equivalents. Accepts a PostgreSQL connection string
/// from the Testcontainers container started in the shared fixture.
/// </summary>
internal class PointsWalletWebApplicationFactory(string connectionString)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // 1. Remove the real database registration added in Program.cs
            //    This ensures no conflict between production and test DbContext
            services.RemoveAll<DbContextOptions<PointsWalletDbContext>>();

            // 2. Register DbContext pointing to the Testcontainers PostgreSQL
            services.AddDbContext<PointsWalletDbContext>(options =>
                options.UseNpgsql(connectionString));

            // 3. Replace authentication so all requests are auto-authenticated
            //    This bypasses real JWT/OAuth without needing tokens in tests
            services.AddAuthentication(defaultScheme: TestAuthHandler.TestScheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.TestScheme,
                    options => { });
        });

        // Use a dedicated environment to avoid triggering dev-only middleware
        // (e.g., auto-migration, Swagger, detailed error pages)
        builder.UseEnvironment("Testing");
    }
}
