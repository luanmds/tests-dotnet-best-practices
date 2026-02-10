using MediatR;

namespace PointsWallet.Domain.Models.Commands;

public sealed record CreateUserCommand(
    string Name,
    string Email
) : IRequest<string>;
