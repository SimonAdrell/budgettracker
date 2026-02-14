# BudgetTrackerApp Copilot Instructions

## What This Is

A **personal budget tracking SPA** built with .NET 10 Aspire, ASP.NET Core Identity, PostgreSQL, and React. Multi-user with role-based account access, transaction import from bank exports, and balance history snapshots.

## Architecture

### Three-Tier Design
1. **AppHost** [BudgetTrackerApp.AppHost/AppHost.cs](../BudgetTrackerApp.AppHost/AppHost.cs): Aspire orchestrator - manages PostgreSQL container, service discovery, health checks
2. **API** [BudgetTrackerApp.ApiService/Program.cs](../BudgetTrackerApp.ApiService/Program.cs): ASP.NET Core with Identity + JWT auth; uses Entity Framework Core with PostgreSQL
3. **Frontend**: React SPA in `frontend/` directory (separate npm project); communicates via HTTP to API

### Key Domain Entities
- **Account**: Bank/investment account (has Name, AccountNumber)
- **Transaction**: Ledger entries linked to Account and Category
- **Category**: Transaction classification (Name, Description, Color for UI)
- **BalanceSnapshot**: Historical balance records for Account on specific date (for trend analysis)
- **AccountUser**: Many-to-many junction with Role field (Owner/Viewer/Editor pattern)

Schema: [docs/database/SCHEMA_OVERVIEW.md](../../docs/database/SCHEMA_OVERVIEW.md)

## Project-Specific Patterns

### Service Layer Pattern
All business logic goes in `Services/` with matching `IService` interface:
- [IAccountService.cs](../BudgetTrackerApp.ApiService/Services/IAccountService.cs): Account management + user access checks
- [IBalanceSnapshotService.cs](../BudgetTrackerApp.ApiService/Services/IBalanceSnapshotService.cs): Snapshot generation
- [IImportService.cs](../BudgetTrackerApp.ApiService/Services/IImportService.cs): Bank CSV import parsing

Services are registered in Program.cs and injected into Controllers. **Always validate user has access** via `UserHasAccessToAccountAsync()` before returning data.

### Controller Endpoints Pattern
Controllers live in [Controllers/](../BudgetTrackerApp.ApiService/Controllers/) and use:
```csharp
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class AccountsController : ControllerBase
{
    // Extract UserId via: var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    // Call service: var account = await _accountService.GetAccountByIdAsync(id, userId);
}
```
All endpoints **must** use `[Authorize]` and verify access. Return `Forbid()` if user lacks access.

### Authentication Flow
- **Login**: `POST /api/auth/login` returns JWT (short-lived) + RefreshToken (long-lived in DB)
- **Refresh**: `POST /api/auth/refresh` exchanges RefreshToken for new JWT
- **JWT Config**: Keys in `appsettings.json` (Jwt:Key, Jwt:Issuer, Jwt:Audience); Identity options set in Program.cs
- React frontend stores JWT in localStorage and includes in all API calls

### Database Access
- DbContext: [ApplicationDbContext.cs](../BudgetTrackerApp.ApiService/Data/ApplicationDbContext.cs) extends `IdentityDbContext<ApplicationUser>`
- Migrations auto-run on startup via AppHost
- Npgsql configured in Program.cs: `builder.AddNpgsqlDbContext<ApplicationDbContext>("identitydb")`
- PostgreSQL container managed by Aspire with named volume `identitydb-volume` for persistence

### DTOs
Request/response objects in [DTOs/](../BudgetTrackerApp.ApiService/DTOs/):
- [AuthDTOs.cs](../BudgetTrackerApp.ApiService/DTOs/AuthDTOs.cs): LoginRequest, RegisterRequest, TokenResponse
- [ImportDTOs.cs](../BudgetTrackerApp.ApiService/DTOs/ImportDTOs.cs): CSV import models

Use DTOs to decouple API contracts from database models.

## Essential Commands

```powershell
# Start entire app (Aspire orchestrates all services + PostgreSQL)
cd BudgetTrackerApp/BudgetTrackerApp.AppHost
dotnet run

# Run tests (creates isolated Aspire environment)
dotnet test BudgetTrackerApp/BudgetTrackerApp.Tests/

# Build check (treats warnings as errors per Directory.Build.props)
dotnet build
```

**Never** run services individually unless debugging a specific service—AppHost handles wiring.

## Code Style (Non-Negotiable)

- **Nullable types**: `<Nullable>enable</Nullable>` in csproj; non-nullable by default, `?` for nullable
- **Warnings → Errors**: `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`; no suppression without justification
- **Implicit usings**: No need for manual `using` statements
- **Primary constructors** (C# 12): `public class Service(ILogger<Service> logger)`
- **Records for DTOs**: `public record LoginRequest(string Email, string Password);`

## File Organization

```
ApiService/
  Controllers/        → HTTP endpoints (one file per entity type)
  Services/           → Business logic (interface + implementation)
  DTOs/               → Request/response contracts
  Models/             → EF Core entities
  Data/               → DbContext + migrations
  Program.cs          → Service registration, middleware
```

## Critical Gotchas

1. **User Access Checks**: Every endpoint **must** verify `UserHasAccessToAccountAsync()` or similar—don't leak data across user boundaries
2. **Service Discovery Name**: "apiservice" must match both AppHost definition AND HttpClient base address in Web/Program.cs
3. **Migrations**: Changes to Models require `dotnet ef migrations add` + commit; AppHost auto-applies on startup
4. **PostgreSQL State**: Container persists to `identitydb-volume` Docker volume; stopping AppHost keeps data
5. **React Build**: Separate `npm run build` in `frontend/` dir; not included in .NET build

## When Adding Endpoints

1. Create/update Controller in [Controllers/](../BudgetTrackerApp.ApiService/Controllers/)
2. Call service (check user access inside service or controller)
3. Return DTOs, not models
4. Add `[Authorize]` attribute
5. Update [API_REFERENCE.md](../../API_REFERENCE.md) with endpoint signature
6. Write test in [BudgetTrackerApp.Tests/](../BudgetTrackerApp.Tests/)

## Debugging

- **Aspire Dashboard**: `http://localhost:15xxx` (port in console); shows resource health, logs, traces
- **Database**: `psql` into running container or use EF Core migrations for schema inspection
- **JWT Decode**: Copy token from API response, paste into [jwt.io](https://jwt.io) to verify claims
- **Logs**: Check controller action + service method for early returns/forbids
