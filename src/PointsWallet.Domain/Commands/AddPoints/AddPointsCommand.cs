using MediatR;

namespace PointsWallet.Domain.Commands.AddPoints;

public sealed record AddPointsCommand(
    string WalletId,
    string UserId,
    long Points,
    string CorrelationId
) : IRequest;
