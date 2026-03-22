using Microsoft.Extensions.Configuration;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? 
	throw new InvalidOperationException("ASPNETCORE_ENVIRONMENT is not set.");

var builder = DistributedApplication.CreateBuilder(args);

// Add infrastructure dependencies
var postgres = builder.AddPostgres("postgres", port: 5432)
	.WithImage("postgres:16-alpine");
	
if (builder.Configuration.GetValue("UseVolumes", true))
{
    postgres.WithDataVolume("postgres_pointswallet_data");
}

var postgresdb = postgres.AddDatabase("pointswalletdb");

var rabbitMq = builder.AddRabbitMQ("rabbitmq")
	.WithImage("rabbitmq:3-management-alpine");

// Add application projects
builder.AddProject<Projects.PointsWallet_Api>("api")
	.WithEnvironment("ASPNETCORE_ENVIRONMENT", environment)
	.WaitFor(postgresdb)
	.WaitFor(rabbitMq)
	.WithHttpHealthCheck("/health")
    .WithReference(postgresdb)
	.WithReference(rabbitMq);

builder.AddProject<Projects.PointsWallet_Worker>("worker")
	.WithEnvironment("ASPNETCORE_ENVIRONMENT", environment)
	.WaitFor(postgresdb)
	.WaitFor(rabbitMq)
    .WithReference(postgresdb)
	.WithReference(rabbitMq);

builder.Build().Run();
