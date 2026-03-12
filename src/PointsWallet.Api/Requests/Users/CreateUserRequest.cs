namespace PointsWallet.Api.Requests.Users;

public sealed record CreateUserRequest(
    string Name,
    string Email
);
