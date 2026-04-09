# CLAUDE.md — BudgetTracker AI Assistant Guide

This file provides AI assistants with the context needed to work effectively in this repository.

---

## Repository Overview

BudgetTracker is a full-stack personal finance application built with:
- **Backend**: ASP.NET Core 10 + Entity Framework Core + PostgreSQL, orchestrated via .NET Aspire
- **Frontend**: React 19 + Vite (primary product UI)
- **Auth**: ASP.NET Core Identity + JWT + Refresh Tokens

The primary product surface is the **React app** at `BudgetTrackerApp/frontend`. The secondary Blazor frontend at `BudgetTrackerApp.Web` is out of scope unless a task explicitly requests it.

---

## Project Structure

```
budgettracker/
├── BudgetTrackerApp/
│   ├── BudgetTrackerApp.ApiService/     # ASP.NET Core backend API
│   │   ├── Controllers/                 # API endpoint handlers
│   │   ├── Services/                    # Business logic (interface-based)
│   │   ├── Models/                      # EF Core domain entities
│   │   ├── DTOs/                        # Request/response contracts
│   │   └── Data/                        # DbContext + Migrations
│   ├── BudgetTrackerApp.AppHost/        # Aspire orchestration host
│   ├── BudgetTrackerApp.ServiceDefaults/# Shared Aspire configuration
│   ├── BudgetTrackerApp.Tests/          # xUnit integration + unit tests
│   ├── BudgetTrackerApp.Web/            # Blazor frontend (OUT OF SCOPE)
│   ├── frontend/                        # React MVP UI (PRIMARY TARGET)
│   │   ├── src/pages/                   # Page components (+ co-located CSS)
│   │   ├── src/services/               # API client modules
│   │   ├── src/App.jsx                  # Router + protected routes
│   │   └── src/main.jsx                # Entry point
│   └── BudgetTrackerApp.sln
├── docs/                                # Design documentation
├── AGENT_GUIDE.md                       # Phase-specific agent rules
├── ARCHITECTURE.md                      # System design notes
├── API_REFERENCE.md                     # API endpoint reference
├── PROJECT_STATUS.md                    # Current phase + risks
└── PROJECT_TASKS.md                     # Planned work items
```

---

## Tech Stack

### Backend
| Concern | Technology |
|---|---|
| Framework | ASP.NET Core 10.0 |
| ORM | Entity Framework Core 10.0 |
| Database | PostgreSQL 13+ (containerized via Aspire) |
| Auth | ASP.NET Core Identity + JWT Bearer + Refresh Tokens |
| Orchestration | .NET Aspire 13.1 |
| API Docs | Scalar 2.12 (OpenAPI) |
| Excel Import | ExcelDataReader 3.7 |
| Testing | xUnit v3 + Moq + EF InMemory |

### Frontend
| Concern | Technology |
|---|---|
| UI Library | React 19.2 |
| Routing | React Router DOM 7 |
| HTTP Client | Axios 1.13 |
| Build Tool | Vite 7.2 |
| Linting | ESLint 9.39 |

---

## Running the Application

### Prerequisites
- .NET 10.0 SDK
- Docker Desktop (running — required for PostgreSQL)
- Node.js 18+

### Start the Full Stack

**Terminal 1 — Backend + Aspire:**
```bash
cd BudgetTrackerApp/BudgetTrackerApp.AppHost
dotnet run
```
Aspire starts PostgreSQL, runs migrations, and starts the API. Note the API port from console output.

**Terminal 2 — React Frontend:**
```bash
cd BudgetTrackerApp/frontend
npm install   # first time only
npm run dev
```
Frontend available at `http://localhost:5173`.

### Run Backend Tests
```bash
cd BudgetTrackerApp
dotnet test
```

### Run Frontend Linting
```bash
cd BudgetTrackerApp/frontend
npm run lint
```

---

## Development Workflows

### Adding a Backend Feature

1. **Add/update domain model** in `Models/`
2. **Create migration** if schema changed:
   ```bash
   cd BudgetTrackerApp/BudgetTrackerApp.ApiService
   dotnet ef migrations add MigrationName --output-dir Data/Migrations
   ```
3. **Add service interface + implementation** in `Services/`
4. **Register service** in `Services/ServiceRegistration.cs`
5. **Add controller** in `Controllers/` with `[Authorize]` and proper `[ProducesResponseType]` attributes
6. **Add DTOs** in `DTOs/` — use `ServiceResponse<T>` wrapper for responses
7. **Write tests** in `BudgetTrackerApp.Tests/`

### Adding a Frontend Feature

1. **Add service module** in `src/services/` for API calls
2. **Create page component** in `src/pages/` with a co-located `.css` file
3. **Register route** in `src/App.jsx`
4. **Use local state** (`useState`) — no global state manager in v1

### Database Migrations

Migrations run automatically on startup. For manual control:
```bash
# Apply
dotnet ef database update

# List
dotnet ef migrations list

# New migration
dotnet ef migrations add YourName --output-dir Data/Migrations

# Remove last migration (unapplied only)
dotnet ef migrations remove
```

> EF tools require the AppHost to be running (PostgreSQL must be available).

---

## Key Conventions

### Backend (.NET)

- **Namespaces**: `BudgetTrackerApp.ApiService.{Layer}` (Controllers, Services, Models, DTOs, Data)
- **Service pattern**: Define an interface (`IFooService`), implement it, register in `ServiceRegistration.cs`
- **Async**: All DB and I/O operations use `async/await` with `CancellationToken` propagation
- **Response wrapper**: Use `ServiceResponse<T>` in `DTOs/ServiceResponse.cs` for consistent API responses
- **Authorization**: Annotate controllers with `[Authorize]`; use `ServiceGuard` for per-resource access checks
- **Routing**: Attribute routing — `[Route("api/[controller]")]` + `[HttpGet]` etc.
- **OpenAPI**: Add `[ProducesResponseType]` attributes to document all response codes

### Frontend (React)

- **Co-located CSS**: Every page has `PageName.jsx` + `PageName.css` in the same directory
- **Services layer**: All API calls go through `src/services/` modules — never call `apiClient` directly from components
- **API client**: `src/services/apiClient.js` is an Axios instance with automatic JWT refresh on 401; import it in service modules
- **Local state only**: Use `useState` and `useRef` — no Redux or Context for app state in v1
- **Race condition guard**: Use a sequence counter (`useRef`) when fetching data that depends on user selection to discard stale responses
- **Formatting**: Use `Intl.NumberFormat` for currency and `Intl.DateTimeFormat` for dates
- **Effect cleanup**: Always return cleanup functions from `useEffect` to cancel in-flight requests

### Authentication

- JWT access tokens expire in **60 minutes** (configurable in `appsettings.json`)
- Refresh tokens expire in **7 days**, stored in the database
- Token rotation: each refresh issues a new access + refresh token pair and revokes the old one
- Password requirements: 6+ chars, at least one uppercase, lowercase, and digit
- Account lockout after 5 failed login attempts

---

## Database Schema

### Entities

| Entity | Key Fields | Notes |
|---|---|---|
| `ApplicationUser` | Id (GUID), UserName, Email, FirstName, LastName | Extends ASP.NET Identity |
| `Account` | Id, Name, AccountNumber | Bank account |
| `AccountUser` | UserId, AccountId, Role | Junction table; Role: Owner/ReadOnly/ReadWrite |
| `Transaction` | AccountId, BookingDate, Amount, Balance, Description | Indexed on TransactionDate |
| `Category` | Name, ParentCategoryId | Self-referencing hierarchy |
| `BalanceSnapshot` | AccountId, SnapshotDate, Balance | Unique per (AccountId, SnapshotDate) |
| `RefreshToken` | UserId, Token, ExpiresAt, IsRevoked | Unique index on Token |

### Design Rules
- `Amount` and `Balance` stored as `decimal(18,2)`
- Snapshots are generated after import; they are not the source of truth for v1 dashboard data — use transactions directly
- `AccountUser.Role` controls access; enforce via `ServiceGuard`

---

## API Endpoints

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | No | Register new user |
| POST | `/api/auth/login` | No | Login, returns JWT + refresh token |
| POST | `/api/auth/refresh` | No | Rotate tokens |
| POST | `/api/auth/logout` | Yes | Revoke refresh tokens |
| GET | `/api/accounts` | Yes | List user's accounts |
| POST | `/api/accounts` | Yes | Create account |
| GET | `/api/dashboard/{accountId}` | Yes | Account dashboard summary |
| GET | `/api/transactions` | Yes | List transactions (paginated) |
| POST | `/api/import` | Yes | Upload Excel file |
| POST | `/api/snapshots/{accountId}/generate` | Yes | Generate balance snapshots |

Full reference: [API_REFERENCE.md](API_REFERENCE.md)

---

## Current Development Phase

See [PROJECT_STATUS.md](PROJECT_STATUS.md) and [AGENT_GUIDE.md](AGENT_GUIDE.md) for the active phase details. In summary:

**Dashboard v1** — Complete:
- `GET /api/dashboard/{accountId}` endpoint
- React `/dashboard` page with account selector
- Balance summary, transaction count, recent transactions (up to 8)
- Empty-state handling (no accounts, no transactions)

**Transfer Verification v1** — Upcoming:
- User-confirmed links between matching transactions across accounts
- Separate transfer-link concept (no mutation of raw transactions)
- Visible transfer markers in the UI
- Prerequisite for future multi-account totals

---

## Current Phase Rules (Transfer Verification v1)

- Transfer verification must be **user-approved** — no auto-confirmation
- Verified transfers must be **visibly marked** in the UI
- Raw `Transaction` records must **remain unchanged** as source of truth
- Use a **separate transfer-link entity** rather than mutating transactions
- Do not implement multi-account totals in this phase
- Scope: candidate rules, verify/unverify behavior, transfer visibility, storage only
- Do not add confidence scoring, advanced matching, or broad automation

---

## Parallelism and Merge Safety

> From [AGENT_GUIDE.md](AGENT_GUIDE.md):

Files that are **unsafe to modify in parallel** (converge on the same files):
- `frontend/src/pages/Dashboard.jsx`
- `frontend/src/pages/Dashboard.css`
- `frontend/src/App.jsx`

Files that are **safe to parallelize** (after the backend contract is merged):
- Backend API tests
- Frontend service modules (e.g., `transactionService.js`)
- New page shell components (no overlap with Dashboard)

---

## Areas Requiring Human Review

Always flag for manual review:
- Login redirect changes or route protection updates
- Account access enforcement logic
- Dashboard data correctness (balance, transaction count)
- Stale data behavior when switching accounts
- Any authentication or token handling changes

---

## CI/CD

GitHub Actions (`.github/workflows/build.yml`) runs on push/PR to `main`:
1. Setup .NET 10.x
2. `dotnet restore`
3. `dotnet build --no-restore`
4. `dotnet test --no-build`

Working directory for all CI steps: `./BudgetTrackerApp`

There is no frontend CI step — lint and build are run manually during development.

---

## Key Files Quick Reference

| File | Purpose |
|---|---|
| `BudgetTrackerApp.ApiService/Program.cs` | App startup, DI, middleware, route mapping |
| `BudgetTrackerApp.ApiService/Data/ApplicationDbContext.cs` | EF Core DbContext and model configuration |
| `BudgetTrackerApp.ApiService/Services/ServiceRegistration.cs` | DI registrations for all services |
| `BudgetTrackerApp.ApiService/Services/ServiceGuard.cs` | Per-resource authorization checks |
| `BudgetTrackerApp.ApiService/DTOs/ServiceResponse.cs` | API response wrapper type |
| `frontend/src/App.jsx` | React router, protected routes, auth redirects |
| `frontend/src/services/apiClient.js` | Axios instance with JWT refresh interceptor |
| `frontend/vite.config.js` | Vite config; proxies `/api` to the Aspire API service |
| `BudgetTrackerApp.AppHost/Program.cs` | Aspire service wiring (PostgreSQL, API, frontends) |

---

## Additional Documentation

- [AGENT_GUIDE.md](AGENT_GUIDE.md) — Phase-specific agent rules and parallelism guidance
- [ARCHITECTURE.md](ARCHITECTURE.md) — System design and planned phases
- [API_REFERENCE.md](API_REFERENCE.md) — Full API endpoint reference with examples
- [IDENTITY_SETUP.md](IDENTITY_SETUP.md) — Auth implementation details
- [docs/database/ER_DIAGRAM.md](docs/database/ER_DIAGRAM.md) — Full ER diagram with rationale
- [docs/database/SCHEMA_OVERVIEW.md](docs/database/SCHEMA_OVERVIEW.md) — Quick schema reference
- [PROJECT_STATUS.md](PROJECT_STATUS.md) — Current state and risks
- [PROJECT_TASKS.md](PROJECT_TASKS.md) — Planned work items
