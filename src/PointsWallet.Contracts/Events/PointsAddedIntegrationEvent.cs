namespace PointsWallet.Contracts.Events;

public sealed record PointsAddedIntegrationEvent(
    string WalletId,
    string UserId,
    long PointsAdded,
    long NewBalance,
    DateTime OccurredAt
);
