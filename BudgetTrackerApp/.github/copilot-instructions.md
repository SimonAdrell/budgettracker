# BudgetTrackerApp Copilot Instructions

## Architecture Overview

This is a **microservices-based .NET 10.0 Aspire application** with three service tiers:

### Service Structure
- **BudgetTrackerApp.AppHost**: Orchestrator using Aspire's `DistributedApplication` - defines service discovery, health checks, and startup coordination (see [AppHost.cs](../BudgetTrackerApp.AppHost/AppHost.cs))
- **BudgetTrackerApp.Web**: Frontend - Blazor Web App with interactive server rendering, communicates with API via `WeatherApiClient` (see [Program.cs](../BudgetTrackerApp.Web/Program.cs))
- **BudgetTrackerApp.ApiService**: Backend - ASP.NET Core API with OpenAPI/Swagger, currently exposes `/weatherforecast` endpoint (see [Program.cs](../BudgetTrackerApp.ApiService/Program.cs))
- **BudgetTrackerApp.ServiceDefaults**: Shared configuration - centralizes Aspire setup, OpenTelemetry, service discovery, resilience handlers (see [Extensions.cs](../BudgetTrackerApp.ServiceDefaults/Extensions.cs))

### Cross-Service Communication
Services use **service discovery with resilience by default** via `AddServiceDiscovery()` and `AddStandardResilienceHandler()`. The Web frontend references ApiService using the discovery scheme `https+http://apiservice` (prefers HTTPS, falls back to HTTP).

## Project-Specific Patterns

### Service Registration Pattern
All services register common defaults via `builder.AddServiceDefaults()`:
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();  // Configures health checks, OpenTelemetry, resilience
```
This is mandatory for every service. See [Extensions.cs](../BudgetTrackerApp.ServiceDefaults/Extensions.cs) for what gets configured.

### Health Checks
All services expose `/health` (liveness) and `/alive` endpoints. AppHost orchestrator waits for health readiness before starting dependent services via `.WaitFor(apiService)`.

### Build Configuration (Directory.Build.props)
- Target framework: **net10.0**
- **Nullable reference types enabled** (`<Nullable>enable</Nullable>`)
- **Treat warnings as errors** (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`)
- **Implicit usings enabled** - no need for explicit `using` statements for common namespaces

## Key Workflows

### Build & Run
```powershell
# Build all projects
dotnet build

# Run via Aspire orchestrator (starts all services)
dotnet run --project BudgetTrackerApp.AppHost

# Run single service (for testing)
dotnet run --project BudgetTrackerApp.Web
```

### Testing
Run integration tests that spin up the full Aspire application:
```powershell
dotnet test BudgetTrackerApp.Tests/BudgetTrackerApp.Tests.csproj
```
Tests use `DistributedApplicationTestingBuilder` to create an isolated test environment with actual service startup (see [WebTests.cs](../BudgetTrackerApp.Tests/WebTests.cs)).

### API Endpoints (Development)
- **Web Frontend**: `https://localhost:5173` (via Aspire)
- **Api Service Swagger**: Navigate to `/openapi/v1.json` when using AppHost (see [ApiService Program.cs](../BudgetTrackerApp.ApiService/Program.cs) - only exposed in Development)

## Code Style & Conventions

- **Nullable-first**: All reference types are non-nullable by default; use `?` for nullable types
- **No bare exceptions**: Code treats warnings as errors; handle all potential exceptions explicitly
- **Primary constructors**: Use C# 12 primary constructor syntax (e.g., `public class WeatherApiClient(HttpClient httpClient)`)
- **Records over classes**: Use `record` for simple data transfer objects (e.g., `WeatherForecast` in [WeatherApiClient.cs](../BudgetTrackerApp.Web/WeatherApiClient.cs))
- **Implicit using statements**: All projects have implicit usings; no need to add standard `using` directives

## File Organization

```
/BudgetTrackerApp.Web/
  Components/           # Razor components (App.razor, Pages/, Layout/)
  WeatherApiClient.cs   # Service-to-service HTTP clients
  Program.cs            # Web service startup configuration

/BudgetTrackerApp.ApiService/
  Program.cs            # API service startup + route definitions

/BudgetTrackerApp.Tests/
  WebTests.cs           # Integration tests using Aspire test builder
```

## Critical Integration Points

1. **Service Discovery**: The Web app discovers the ApiService by hostname `apiservice` (defined in AppHost). Update [AppHost.cs](../BudgetTrackerApp.AppHost/AppHost.cs) when adding new services.
2. **OpenTelemetry**: All services emit traces, logs, and metrics via shared configuration in [Extensions.cs](../BudgetTrackerApp.ServiceDefaults/Extensions.cs#L45-L70).
3. **HttpClient Defaults**: All services apply resilience policies (retry, timeout) globally via `ConfigureHttpClientDefaults()` in [Extensions.cs](../BudgetTrackerApp.ServiceDefaults/Extensions.cs#L33-L40).

## Quick Debugging Tips

- **Service won't start?** Check health check endpoint: `curl https://localhost:[port]/health`
- **Service discovery failing?** Ensure service name in AppHost matches the discovery hostname in HttpClient base address
- **Async streaming pattern**: Web app uses `GetFromJsonAsAsyncEnumerable` for efficient streaming from API (see [WeatherApiClient.cs](../BudgetTrackerApp.Web/WeatherApiClient.cs#L6-L17))
