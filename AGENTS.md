# BudgetTracker Agent Instructions

## Scope

These instructions apply to the entire repository.

## Project Summary

BudgetTracker is a personal budget tracking solution built around .NET Aspire, ASP.NET Core Identity, PostgreSQL, and a React SPA.

Main capabilities:
- Multi-user support with role-based account access
- Transaction and category management
- CSV import from bank exports
- Historical balance snapshots
- JWT + refresh token authentication

## Repository Layout

- `BudgetTrackerApp/`
  - `BudgetTrackerApp.AppHost/`: Aspire orchestrator (starts and wires dependencies)
  - `BudgetTrackerApp.ApiService/`: ASP.NET Core API + Identity + EF Core
  - `BudgetTrackerApp.Web/`: .NET web frontend
  - `BudgetTrackerApp.ServiceDefaults/`: shared Aspire defaults/config
  - `BudgetTrackerApp.Tests/`: tests
  - `frontend/`: React SPA (separate npm project)
- `docs/`: architecture and database documentation
- `API_REFERENCE.md`: API contract documentation
- `IDENTITY_SETUP.md`: auth/identity setup

## Architecture Rules

Three-tier architecture:
1. AppHost orchestrates services and PostgreSQL.
2. API hosts business logic, auth, and data access.
3. Frontends call the API over HTTP.

Domain entities to preserve:
- Account
- Transaction
- Category
- BalanceSnapshot
- AccountUser (with role semantics)

Reference schema: `docs/database/SCHEMA_OVERVIEW.md`

## API and Security Requirements

- All protected endpoints must use `[Authorize]`.
- Enforce user/account access checks before returning account-scoped data.
- Never leak cross-user account data.
- Return `Forbid()` for unauthorized account access (authenticated but not allowed).
- Keep JWT/refresh token flow intact:
  - Login returns JWT + refresh token
  - Refresh endpoint exchanges refresh token for a new JWT

## Backend Coding Conventions

- Keep business logic in `BudgetTrackerApp.ApiService/Services/`.
- Use service interfaces (`I*Service`) and dependency injection.
- Controllers should be thin, delegate to services.
- Use DTOs in `BudgetTrackerApp.ApiService/DTOs/` for API contracts.
- Do not expose EF entities directly when DTOs are expected.

Quality settings:
- Nullable reference types are enabled; respect nullability annotations.
- Warnings are treated as errors; do not ignore warnings without reason.
- Prefer modern C# patterns already used by the project (records for DTOs, concise constructors where appropriate).

## Data and Migrations

- `ApplicationDbContext` is the source of truth for EF mappings.
- Model changes require a migration and migration files committed to source control.
- AppHost/API startup applies migrations automatically; do not duplicate migration logic unnecessarily.

## Frontend Notes

- React app in `BudgetTrackerApp/frontend/` is a separate npm project.
- .NET build does not automatically build React assets.
- Keep API contract usage aligned with `API_REFERENCE.md`.

## Run and Validation Commands

Preferred workflow:

```powershell
cd BudgetTrackerApp/BudgetTrackerApp.AppHost
dotnet run
```

Validation:

```powershell
dotnet build
dotnet test BudgetTrackerApp/BudgetTrackerApp.Tests/
```

When working in React:

```powershell
cd BudgetTrackerApp/frontend
npm run dev
```

## Documentation Update Policy

When behavior or contracts change, update docs in the same change:
- API changes -> `API_REFERENCE.md`
- Auth changes -> `IDENTITY_SETUP.md`
- Data model changes -> `docs/database/` as needed

## Agent Do / Don't

Do:
- Keep changes scoped and consistent with existing architecture.
- Prioritize security and access control correctness.
- Add or update tests for changed behavior when feasible.
- Preserve service discovery naming consistency (for example `apiservice` wiring).

Don't:
- Bypass access checks for convenience.
- Move business logic into controllers.
- Introduce breaking API changes without updating documentation.
- Run services individually by default when AppHost orchestration is the intended path.

