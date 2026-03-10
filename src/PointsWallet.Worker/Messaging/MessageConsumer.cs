using MassTransit;
using MediatR;
using PointsWallet.Contracts;

namespace PointsWallet.Worker.Messaging;

/// <summary>
/// Single MassTransit consumer that handles any <see cref="IMessage"/> received from the broker.
/// Uses the <see cref="MessageHandlerRegistry"/> to resolve the concrete message type via
/// <c>ConsumeContext.TryGetMessage&lt;T&gt;()</c>, finds the matching
/// <see cref="IMessageCommandMapper{TMessage}"/>, and dispatches the resulting command through MediatR.
/// <para>
/// To add support for a new message type:
/// <list type="number">
///   <item>Create a message record in <c>Contracts/Messages</c> implementing <see cref="IMessage"/></item>
///   <item>Create a command + handler in <c>Domain/Commands</c></item>
///   <item>Create an <see cref="IMessageCommandMapper{TMessage}"/> implementation</item>
///   <item>Register the message type via <c>AddMessageHandler&lt;TMessage, TMapper&gt;()</c></item>
/// </list>
/// </para>
/// </summary>
public sealed class MessageConsumer(
    MessageHandlerRegistry registry,
    IServiceProvider serviceProvider,
    IMediator mediator,
    ILogger<MessageConsumer> logger) : IConsumer<IMessage>
{
    public async Task Consume(ConsumeContext<IMessage> context)
    {
        logger.LogInformation(
            "[Message Consumer] Received message with CorrelationId {CorrelationId}",
            context.Message.CorrelationId);

        foreach (var handler in registry.Handlers)
        {
            var handled = await handler.TryHandleAsync(
                context, serviceProvider, mediator, context.CancellationToken);

            if (handled)
            {
                logger.LogInformation(
                    "[Message Consumer] Successfully processed {MessageType} with CorrelationId {CorrelationId}",
                    handler.MessageTypeName,
                    context.Message.CorrelationId);
                return;
            }
        }

        logger.LogWarning(
            "[Message Consumer] No handler found for message with CorrelationId {CorrelationId}. "
            + "Supported message types: {SupportedTypes}",
            context.Message.CorrelationId,
            string.Join(", ", context.SupportedMessageTypes));
    }
}
