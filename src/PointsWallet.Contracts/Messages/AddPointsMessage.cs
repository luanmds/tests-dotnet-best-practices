namespace PointsWallet.Contracts.Messages;

public sealed record AddPointsMessage(
    string WalletId,
    string UserId,
    long Points,
    string CorrelationId
) : IMessage;
