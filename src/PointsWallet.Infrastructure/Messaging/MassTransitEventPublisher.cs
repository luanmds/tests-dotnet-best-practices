using MassTransit;
using PointsWallet.Domain.Events;

namespace PointsWallet.Infrastructure.Messaging;

public sealed class MassTransitEventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        await publishEndpoint.Publish(@event, cancellationToken);
    }
}
