using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// Add infrastructure dependencies
var postgres = builder.AddPostgres("postgres", port: 5432)
	.WithImage("postgres:16-alpine")
    .WithEnvironment("POSTGRES_USER", "postgres")
    .WithEnvironment("POSTGRES_DB", "pointswalletdb");
	
if (builder.Configuration.GetValue("UseVolumes", true))
{
    postgres.WithDataVolume("postgres_pointswallet_data");
}

var postgresdb = postgres.AddDatabase("pointswalletdb");

var rabbitMq = builder.AddRabbitMQ("rabbitmq")
	.WithImage("rabbitmq:3-management-alpine");

// Add application projects
builder.AddProject<Projects.PointsWallet_Api>("api")
	.WaitFor(postgresdb)
	.WaitFor(rabbitMq)
	.WithHttpHealthCheck("/health")
    .WithReference(postgresdb)
	.WithReference(rabbitMq);

builder.AddProject<Projects.PointsWallet_Worker>("worker")
	.WaitFor(postgresdb)
	.WaitFor(rabbitMq)
    .WithReference(postgresdb)
	.WithReference(rabbitMq);

builder.Build().Run();
