using Microsoft.IdentityModel.Tokens;
using PointsWallet.Api.Requests.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PointsWallet.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth")
            .WithName("Auth")
            .WithOpenApi();

        group.MapPost("/dev-token", CreateDevToken)
            .WithName("CreateDevToken")
            .WithOpenApi()
            .Produces<CreateDevTokenResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static IResult CreateDevToken(
        CreateDevTokenRequest request,
        IConfiguration configuration)
    {
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        var key = configuration["Jwt:Key"];
        var expiresInMinutes = int.TryParse(configuration["Jwt:ExpiresInMinutes"], out var minutes)
            ? minutes
            : 60;

        if (issuer is null || audience is null || key is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "JWT settings are not configured");
        }

        var now = DateTime.UtcNow;
        var userId = string.IsNullOrWhiteSpace(request.UserId)
            ? Guid.NewGuid().ToString()
            : request.UserId;
        var name = string.IsNullOrWhiteSpace(request.Name)
            ? "dev-user"
            : request.Name;
        var email = string.IsNullOrWhiteSpace(request.Email)
            ? "dev-user@pointswallet.local"
            : request.Email;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Name, name),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, "User"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            notBefore: now,
            expires: now.AddMinutes(expiresInMinutes),
            signingCredentials: credentials);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return Results.Ok(new CreateDevTokenResponse(
            jwt,
            "Bearer",
            now.AddMinutes(expiresInMinutes)));
    }
}

public sealed record CreateDevTokenResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc
);
