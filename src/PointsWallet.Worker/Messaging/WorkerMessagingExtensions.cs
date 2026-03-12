using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using PointsWallet.Contracts;

namespace PointsWallet.Worker.Messaging;

/// <summary>
/// Extension methods for registering the unified <see cref="MessageConsumer"/>
/// with its message-type handlers and command mappers.
/// </summary>
public static class WorkerMessagingExtensions
{
    /// <summary>
    /// Registers the <see cref="MessageHandlerRegistry"/> and returns a builder
    /// for fluent registration of message types and their mappers.
    /// <para>
    /// Usage:
    /// <code>
    /// builder.Services
    ///     .AddMessageConsumer()
    ///     .HandleMessage&lt;AddPointsMessage, AddPointsMessageMapper&gt;();
    /// </code>
    /// </para>
    /// </summary>
    public static MessageConsumerBuilder AddMessageConsumer(this IServiceCollection services)
    {
        var registry = new MessageHandlerRegistry();
        services.AddSingleton(registry);
        return new MessageConsumerBuilder(services, registry);
    }
}

/// <summary>
/// Fluent builder for registering message types and their <see cref="IMessageCommandMapper{TMessage}"/> implementations.
/// Each call to <see cref="HandleMessage{TMessage,TMapper}"/> registers the mapper in DI
/// and adds a typed dispatch handler to the <see cref="MessageHandlerRegistry"/>.
/// </summary>
public sealed class MessageConsumerBuilder(
    IServiceCollection services,
    MessageHandlerRegistry registry)
{
    /// <summary>
    /// Registers a message type with its corresponding mapper.
    /// </summary>
    /// <typeparam name="TMessage">The message type consumed from the broker.</typeparam>
    /// <typeparam name="TMapper">
    /// The <see cref="IMessageCommandMapper{TMessage}"/> implementation that maps the message to a command.
    /// </typeparam>
    /// <returns>This builder instance for fluent chaining.</returns>
    public MessageConsumerBuilder HandleMessage<TMessage, TMapper>()
        where TMessage : class, IMessage
        where TMapper : class, IMessageCommandMapper<TMessage>
    {
        registry.Register<TMessage>();
        services.AddScoped<IMessageCommandMapper<TMessage>, TMapper>();
        return this;
    }

    /// <summary>
    /// Configures MassTransit with the unified <see cref="MessageConsumer"/>.
    /// Call this after all message types have been registered.
    /// </summary>
    /// <param name="configureBus">Optional additional MassTransit bus configuration.</param>
    /// <returns>The service collection for further chaining.</returns>
    public static IServiceCollection WithMassTransit(
        IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureBus = null)
    {
        return Infrastructure.Messaging.MessagingExtensions.AddMessaging(
            services,
            configuration,
            busConfigurator =>
            {
                busConfigurator.AddConsumer<MessageConsumer>();
                configureBus?.Invoke(busConfigurator);
            });
    }
}
