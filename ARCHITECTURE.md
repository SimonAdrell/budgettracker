# Architecture

## Primary Decisions

- Keep the current Aspire + `BudgetTrackerApp.ApiService` + PostgreSQL + React backbone.
- Treat `BudgetTrackerApp/frontend` as the primary MVP UI.
- Treat `BudgetTrackerApp.Web` as secondary and out of scope unless a task explicitly requests work there.
- Build the MVP by extending existing seams instead of redesigning auth, persistence, or project structure.

## Current Shape

The current architecture is a good fit for incremental delivery:

- `BudgetTrackerApp.AppHost` orchestrates PostgreSQL, the API service, the Blazor frontend, and the Vite React app.
- `BudgetTrackerApp.ApiService` contains the domain/service logic and the main backend surface.
- EF Core persistence already models users, refresh tokens, accounts, account-user links, categories, transactions, and balance snapshots.
- `BudgetTrackerApp.Tests` already provides meaningful automated coverage and is a strong base for agent-driven work.

## What Stays

- The existing database model shape should stay.
- The current auth foundation should stay.
- The import pipeline should stay as the working ingestion path for the MVP.
- The snapshot subsystem should stay as the source for balance-history derivation.
- Aspire orchestration should stay as the local development story.

## What Should Be Simplified Now

- Standardize frontend API access early so agents are not duplicating route logic across services.
- Keep feature work focused on the React app until the MVP is complete.
- Add only the minimum read/query endpoints needed to support transaction viewing and a simple account summary.

## What Should Wait

These are good candidates for later refactoring, not now:

- normalizing the backend endpoint/module style across `Program.cs` mappings and controller-based endpoints
- redesigning the import subsystem into a broader plugin architecture
- broader UI redesign or dual-frontend parity
- large-scale endpoint-style cleanup before the MVP user flow works

## Technical Constraints

- Do not assume the modeled feature set is fully exposed. The schema is ahead of the visible UI/API.
- Do not conflate `BookingDate` and `TransactionDate`. Duplicate detection and snapshot generation rely on date semantics that are easy to damage.
- Do not weaken access scoping. Transaction and account reads must stay tied to accounts the current user can access.
- Do not assume the frontend auth flow is complete just because the backend supports refresh tokens.
- Treat auto-applied migrations on startup as a development convenience that needs careful review when schema work is involved.

## Known Fragile Areas

- Import duplicate detection
- Balance calculations and same-day ordering
- Snapshot regeneration scope after import
- Token refresh and logout behavior
- Frontend route/base-path consistency
- Dual-frontend ambiguity in AppHost

## Explicit Deferrals

The following are intentionally deferred until after the MVP is usable:

- category hierarchy and richer category UX
- budget rules and monthly planning
- richer analytics and charts
- account-sharing UI
- multiple importer plugins
- advanced reporting/export
- production-grade observability and deployment concerns

## Anti-Goals For Now

- Do not build the same product UI twice in Blazor and React.
- Do not redesign auth or persistence.
- Do not add AI or "smart" categorization features.
- Do not start with broad refactors before the core "import and view your data" flow works.
