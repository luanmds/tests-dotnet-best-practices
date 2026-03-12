using MediatR;

namespace PointsWallet.Domain.Commands.CreateUser;

public sealed record CreateUserCommand(
    string Name,
    string Email
) : IRequest<string>;
