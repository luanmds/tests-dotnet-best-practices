using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PointsWallet.Domain.Events;

namespace PointsWallet.Infrastructure.Messaging;

public static class MessagingExtensions
{
    /// <summary>
    /// Registers MassTransit with RabbitMQ transport and the IEventPublisher abstraction.
    /// Consumer registration should be done separately by each host (API or Worker).
    /// </summary>
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        services.AddMassTransit(busConfigurator =>
        {
            configureConsumers?.Invoke(busConfigurator);

            busConfigurator.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqConnString = configuration.GetConnectionString("rabbitmq") ?? 
                    throw new InvalidOperationException("RabbitMQ connection string not found.");

                cfg.Host(new Uri(rabbitMqConnString));

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

        return services;
    }
}
