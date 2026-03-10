namespace PointsWallet.Contracts;

/// <summary>
/// Base interface for all messages exchanged through the message broker.
/// Every message must carry a correlation ID for distributed tracing.
/// </summary>
public interface IMessage
{
    string CorrelationId { get; }
}
