using PointsWallet.Api.Endpoints;
using PointsWallet.Domain;
using PointsWallet.Domain.Behaviors;
using PointsWallet.Domain.Models.Commands;
using PointsWallet.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Domain and Infrastructure layers
builder.Services.AddDomain();
builder.Services.AddInfrastructure();

// Register MediatR with validation behavior
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<CreateUserCommand>();
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Map endpoints
app.MapUserEndpoints();

app.Run();
