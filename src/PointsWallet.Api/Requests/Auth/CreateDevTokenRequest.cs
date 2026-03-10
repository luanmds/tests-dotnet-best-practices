namespace PointsWallet.Api.Requests.Auth;

public sealed record CreateDevTokenRequest(
    string? UserId,
    string? Name,
    string? Email
);
