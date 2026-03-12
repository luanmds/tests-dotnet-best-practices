// =============================================================================
// Test Authentication Handler — Bypasses real auth in integration tests
// =============================================================================
// This handler replaces the real authentication scheme so that all HTTP
// requests in integration tests are automatically authenticated with
// deterministic claims.
//
// Usage: Registered in WebApplicationFactory.ConfigureTestServices via
//   services.AddAuthentication(defaultScheme: TestAuthHandler.TestScheme)
//       .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(...)
//
// Test classes can reference TestAuthHandler.TestUserId and TestUserEmail
// when setting up test data that needs to match the authenticated user.
// =============================================================================

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string TestScheme = "TestScheme";
    public const string TestUserId = "test-user-id";
    public const string TestUserEmail = "test@example.com";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Build a set of deterministic claims for the test identity.
        // These claims match what a real JWT token would provide.
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId),
            new Claim(ClaimTypes.Email, TestUserEmail),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim("sub", TestUserId)
        };

        var identity = new ClaimsIdentity(claims, TestScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, TestScheme);

        // Always succeed — every request is treated as authenticated
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
