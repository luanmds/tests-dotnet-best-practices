using PointsWallet.Contracts.Messages;
using PointsWallet.Domain;
using PointsWallet.Domain.Behaviors;
using PointsWallet.Domain.Commands.AddPoints;
using PointsWallet.Infrastructure;
using PointsWallet.Infrastructure.Configurations;
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
});

// Register MediatR with validation behavior
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<AddPointsCommand>();
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database", LogLevel.Warning);  

var host = builder.Build();

if ((host.Services.GetRequiredService<IHostEnvironment>().IsDevelopment() || 
     host.Services.GetRequiredService<IHostEnvironment>().EnvironmentName == "Testing")
    && builder.Configuration.GetValue<bool>("UseMigrations"))
{
    using var scope = host.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<PointsWalletDbContext>();
    await dbContext.MigrateDatabaseAsync(CancellationToken.None);
}

await host.RunAsync();
