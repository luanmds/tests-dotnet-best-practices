using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace PointsWallet.AspireIntegrationTests.Fixtures;

public static class JwtTokenFactory
{
    // Must match the API's appsettings.json Jwt section
    private const string Issuer = "PointsWallet.Api";
    private const string Audience = "PointsWallet.Client";
    private const string Key = "pointswallet-super-secret-development-key-2026";

    public const string TestUserId = "test-user-id";
    public const string TestUserEmail = "test@example.com";

    public static string GenerateToken()
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, TestUserId),
            new Claim(JwtRegisteredClaimNames.Email, TestUserEmail),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.NameIdentifier, TestUserId)
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
