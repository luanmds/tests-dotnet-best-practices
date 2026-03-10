using System.Runtime.CompilerServices;
using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PointsWallet.Contracts;

[assembly: InternalsVisibleTo("PointsWallet.UnitTests")]

namespace PointsWallet.Worker.Messaging;

/// <summary>
/// Maintains a registry of typed dispatch delegates for each known message type.
/// Built at startup and used by <see cref="MessageConsumer"/> to resolve the correct
/// <see cref="IMessageCommandMapper{TMessage}"/> and dispatch the resulting command via MediatR.
/// <para>
/// Each registered message type gets a delegate that:
/// <list type="number">
///   <item>Calls <c>ConsumeContext.TryGetMessage&lt;T&gt;()</c> to safely deserialize the concrete type</item>
///   <item>Resolves the corresponding <see cref="IMessageCommandMapper{TMessage}"/> from DI</item>
///   <item>Maps the message to a MediatR command and sends it</item>
/// </list>
/// </para>
/// </summary>
public sealed class MessageHandlerRegistry
{
    private readonly List<MessageHandler> _handlers = [];

    /// <summary>
    /// Gets all registered message handlers.
    /// </summary>
    internal IReadOnlyList<MessageHandler> Handlers => _handlers;

    /// <summary>
    /// Registers a dispatch handler for a specific message type.
    /// </summary>
    /// <typeparam name="TMessage">The concrete message type to register.</typeparam>
    /// <returns>This registry instance for fluent chaining.</returns>
    public MessageHandlerRegistry Register<TMessage>() where TMessage : class, IMessage
    {
        _handlers.Add(new MessageHandler(
            typeof(TMessage).Name,
            async (context, serviceProvider, mediator, cancellationToken) =>
            {
                if (!context.TryGetMessage<TMessage>(out var typedContext))
                    return false;

                var mapper = serviceProvider.GetRequiredService<IMessageCommandMapper<TMessage>>();
                var command = mapper.ToCommand(typedContext.Message);
                await mediator.Send(command, cancellationToken);

                return true;
            }));

        return this;
    }
}

/// <summary>
/// Represents a handler that can attempt to dispatch a message of a specific type.
/// </summary>
/// <param name="MessageTypeName">The human-readable name of the message type for logging.</param>
/// <param name="TryHandleAsync">
/// Delegate that tries to match and dispatch the message.
/// Returns <c>true</c> if the message was handled, <c>false</c> otherwise.
/// </param>
internal sealed record MessageHandler(
    string MessageTypeName,
    Func<ConsumeContext<IMessage>, IServiceProvider, IMediator, CancellationToken, Task<bool>> TryHandleAsync);
