var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL with data volume for persistence
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var identityDb = postgres.AddDatabase("identitydb");

var apiService = builder.AddProject<Projects.BudgetTrackerApp_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(identityDb)
    .WaitFor(identityDb);

// Add React frontend using Node.js
var reactApp = builder.AddNpmApp("react-frontend", "../frontend")
    .WithHttpEndpoint(port: 5173, env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile()
    .WaitFor(apiService);

builder.AddProject<Projects.BudgetTrackerApp_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
