# CLAUDE.md — BudgetTracker

## Stack
- **Backend**: ASP.NET Core 10 + EF Core + PostgreSQL (via .NET Aspire 13.1)
- **Frontend**: React 19 + Vite 7 + Axios + React Router 7
- **Auth**: ASP.NET Core Identity + JWT (60 min) + Refresh Tokens (7 days)
- **Tests**: xUnit v3 + Moq + EF InMemory

## Scope
- Primary UI: `BudgetTrackerApp/frontend` (React) — always target this
- `BudgetTrackerApp.Web` (Blazor) is **out of scope** unless explicitly requested

## Project Layout
```
BudgetTrackerApp/
├── BudgetTrackerApp.ApiService/   # Backend
│   ├── Controllers/               # API handlers
│   ├── Services/                  # Business logic (interface-based)
│   ├── Models/                    # EF Core entities
│   ├── DTOs/                      # Request/response types
│   └── Data/                      # DbContext + Migrations/
├── BudgetTrackerApp.AppHost/      # Aspire orchestration
├── BudgetTrackerApp.Tests/        # xUnit tests
└── frontend/src/
    ├── pages/                     # PageName.jsx + PageName.css (co-located)
    ├── services/                  # API modules (never call apiClient from components)
    └── App.jsx                    # Router + protected routes
```

## Commands
```bash
# Run backend (Terminal 1)
cd BudgetTrackerApp/BudgetTrackerApp.AppHost && dotnet run

# Run frontend (Terminal 2)
cd BudgetTrackerApp/frontend && npm run dev   # http://localhost:5173

# Tests
cd BudgetTrackerApp && dotnet test

# Lint
cd BudgetTrackerApp/frontend && npm run lint

# New migration (AppHost must be running)
cd BudgetTrackerApp/BudgetTrackerApp.ApiService
dotnet ef migrations add Name --output-dir Data/Migrations
```

## Backend Conventions
- Namespace: `BudgetTrackerApp.ApiService.{Controllers|Services|Models|DTOs|Data}`
- Service pattern: `IFooService` interface → implementation → register in `ServiceRegistration.cs`
- All DB/IO: `async/await` with `CancellationToken`
- Responses: wrap in `ServiceResponse<T>` (`DTOs/ServiceResponse.cs`)
- Auth: `[Authorize]` on controllers; `ServiceGuard` for per-resource checks
- Routing: `[Route("api/[controller]")]` + HTTP verb attributes
- Document all status codes with `[ProducesResponseType]`

## Frontend Conventions
- All API calls via `src/services/` modules — never use `apiClient` directly in components
- `apiClient.js`: Axios instance with automatic JWT refresh on 401
- Local state only: `useState` + `useRef` — no Redux/Context
- Race condition guard: sequence counter (`useRef`) for selection-dependent fetches
- Formatting: `Intl.NumberFormat` (currency), `Intl.DateTimeFormat` (dates)
- Always return `useEffect` cleanup functions

## Key Files
| File | Purpose |
|---|---|
| `ApiService/Program.cs` | Startup, DI, middleware |
| `ApiService/Data/ApplicationDbContext.cs` | EF DbContext |
| `ApiService/Services/ServiceRegistration.cs` | DI registrations |
| `ApiService/Services/ServiceGuard.cs` | Resource-level auth |
| `ApiService/DTOs/ServiceResponse.cs` | Response wrapper |
| `frontend/src/App.jsx` | Router + auth redirects |
| `frontend/src/services/apiClient.js` | Axios + JWT refresh |

## API Endpoints
| Method | Path | Auth |
|---|---|---|
| POST | `/api/auth/register` | No |
| POST | `/api/auth/login` | No |
| POST | `/api/auth/refresh` | No |
| POST | `/api/auth/logout` | Yes |
| GET/POST | `/api/accounts` | Yes |
| GET | `/api/dashboard/{accountId}` | Yes |
| GET | `/api/transactions` | Yes |
| POST | `/api/import` | Yes |
| POST | `/api/snapshots/{accountId}/generate` | Yes |

## Current Phase: Transfer Verification v1
- Verification is **user-approved only** — no auto-confirmation
- Use a **separate transfer-link entity** — never mutate `Transaction` records
- Verified transfers must be visibly marked in the UI
- No multi-account totals yet; no confidence scoring or advanced matching

## Parallel Work — Unsafe to Modify Concurrently
- `frontend/src/pages/Dashboard.jsx` / `Dashboard.css`
- `frontend/src/App.jsx`

## Always Flag for Human Review
- Auth/redirect changes, route protection
- Account access enforcement (`ServiceGuard`)
- Dashboard data correctness or stale-data behavior

## CI
GitHub Actions on push/PR to `main`: `dotnet restore` → `dotnet build` → `dotnet test`
Working directory: `./BudgetTrackerApp`. No frontend CI step.

## Further Reading
- [AGENT_GUIDE.md](AGENT_GUIDE.md) — phase rules and parallelism
- [API_REFERENCE.md](API_REFERENCE.md) — full endpoint reference
- [PROJECT_STATUS.md](PROJECT_STATUS.md) — current state and risks
- [docs/database/ER_DIAGRAM.md](docs/database/ER_DIAGRAM.md) — schema
