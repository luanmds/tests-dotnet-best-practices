using PointsWallet.Contracts.Messages;
using PointsWallet.Domain.Commands.AddPoints;

namespace PointsWallet.Worker.Messaging.Mappers;

public sealed class AddPointsMessageMapper : IMessageCommandMapper<AddPointsMessage>
{
    public object ToCommand(AddPointsMessage message) =>
        new AddPointsCommand(
            message.WalletId,
            message.UserId,
            message.Points,
            message.CorrelationId);
}
