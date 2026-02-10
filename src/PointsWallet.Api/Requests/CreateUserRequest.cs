namespace PointsWallet.Api.Requests;

public sealed record CreateUserRequest(
    string Name,
    string Email
);
