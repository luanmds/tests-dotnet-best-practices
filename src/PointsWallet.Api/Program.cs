using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PointsWallet.Api.Endpoints;
using PointsWallet.Domain;
using PointsWallet.Domain.Behaviors;
using PointsWallet.Domain.Commands.CreateUser;
using PointsWallet.Infrastructure;
using PointsWallet.Infrastructure.Messaging;
using System.Text;
using PointsWallet.Infrastructure.Configurations;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddOpenApi();

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme() 
    { 
        Name = "Authorization", 
        Type = SecuritySchemeType.ApiKey, 
        Scheme = "Bearer", 
        BearerFormat = "JWT", 
        In = ParameterLocation.Header
    }); 
    c.AddSecurityRequirement(new OpenApiSecurityRequirement 
    { 
        { 
            new OpenApiSecurityScheme 
            { 
                Reference = new OpenApiReference 
                { 
                    Type = ReferenceType.SecurityScheme, 
                    Id = "Bearer" 
                } 
            },
            Array.Empty<string>()
        } 
    }); 
});

// Add Authentication & Authorization
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is not configured");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is not configured");
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

// Register Domain and Infrastructure layers
builder.Services.AddDomain();
builder.Services.AddInfrastructure(builder.Configuration);

// Register MassTransit + RabbitMQ messaging (publisher only, no consumers)
builder.Services.AddMessaging(builder.Configuration);

// Register MediatR with validation behavior
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<CreateUserCommand>();
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database", LogLevel.Warning);  

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("pointswalletdb")!, 
    name: "PostgreSQL");
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapWalletEndpoints();
app.MapHealthChecks("/health");


if ((app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Testing")
    && builder.Configuration.GetValue<bool>("UseMigrations"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<PointsWalletDbContext>();
    await dbContext.MigrateDatabaseAsync(CancellationToken.None);
}

app.Run();
