var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL with data volume for persistence
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

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
