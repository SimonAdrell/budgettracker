using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);
var postgresPassword = builder.AddParameter(
    "postgres-password",
    builder.Configuration["POSTGRES_PASSWORD"] ?? "postgres",
    secret: true);

// Add PostgreSQL with data volume for persistence
var postgres = builder.AddPostgres("postgres", password: postgresPassword);
if (!builder.Environment.IsEnvironment("Testing"))
{
    postgres = postgres.WithDataVolume();
}

var identityDb = postgres.AddDatabase("identitydb");

var apiService = builder.AddProject<Projects.BudgetTrackerApp_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(identityDb)
    .WaitFor(identityDb);

builder.AddProject<Projects.BudgetTrackerApp_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddViteApp("react-frontend", "../frontend")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithNpm();

builder.Build().Run();
