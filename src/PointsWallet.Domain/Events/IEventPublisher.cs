namespace PointsWallet.Domain.Events;

/// <summary>
/// Abstraction for publishing integration events to a message broker.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class;
}
