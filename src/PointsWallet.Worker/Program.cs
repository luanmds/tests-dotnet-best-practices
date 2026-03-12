using MassTransit;
using PointsWallet.Contracts.Messages;
using PointsWallet.Domain;
using PointsWallet.Domain.Behaviors;
using PointsWallet.Domain.Commands.AddPoints;
using PointsWallet.Infrastructure;
using PointsWallet.Infrastructure.Messaging;
using PointsWallet.Worker.Messaging;
using PointsWallet.Worker.Messaging.Mappers;

var builder = Host.CreateApplicationBuilder(args);

// Register Domain and Infrastructure layers
builder.Services.AddDomain();
builder.Services.AddInfrastructure(builder.Configuration);

// Register the unified message consumer with its message-type handlers
builder.Services
    .AddMessageConsumer()
    .HandleMessage<AddPointsMessage, AddPointsMessageMapper>();

// Register MassTransit + RabbitMQ messaging with the unified consumer
builder.Services.AddMessaging(builder.Configuration, busConfigurator =>
{
    busConfigurator.AddConsumer<MessageConsumer>();
    busConfigurator.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
        cfg.ConfigureEndpoints(context);
    });
});

// Register MediatR with validation behavior
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<AddPointsCommand>();
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database", LogLevel.Warning);  

var host = builder.Build();
host.Run();
