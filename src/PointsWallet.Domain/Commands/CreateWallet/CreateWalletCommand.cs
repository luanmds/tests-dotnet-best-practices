using MediatR;

namespace PointsWallet.Domain.Commands.CreateWallet;

public sealed record CreateWalletCommand(
    string UserId,
    string? SymbolicName
) : IRequest<string>;
