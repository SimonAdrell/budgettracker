# AGENT_GUIDE.md

## Dashboard Phase Rules

The current phase is focused on building a post-login dashboard in `BudgetTrackerApp/frontend`.

### Product Goal
After login, authenticated users should land on `/dashboard` and immediately see useful account information.

### Dashboard v1 Scope
Build only:
- one account-scoped dashboard view
- current balance
- last updated
- transaction count
- recent transactions preview
- no-account state
- no-transactions state
- loading/error states
- simple path to import transactions

Do not build yet:
- charting
- snapshot visualizations
- global sidebar/navigation system
- all-account combined dashboard
- category analytics
- advanced filtering/search
- budget planning features

## Rules for Dashboard Tasks

- Keep dashboard work in `BudgetTrackerApp/frontend`
- Do not modify `BudgetTrackerApp.Web`
- Keep backend changes minimal and additive
- Prefer one endpoint for dashboard v1
- Prefer transaction-derived data over snapshot-read logic for v1
- Avoid introducing global state for dashboard v1
- Keep account selection as local page state initially

## Verification Rules for Dashboard Tasks

For backend dashboard tasks:
- verify endpoint shape
- verify auth behavior
- verify empty-account behavior
- verify populated-account behavior

For frontend dashboard tasks:
- verify authenticated navigation to `/dashboard`
- verify unauthenticated redirect to `/login`
- verify no-account state
- verify no-transaction state
- verify populated-account state
- verify account switching
- verify import → dashboard flow

## Parallel Work Warning

Only a small number of dashboard tasks are safe to run in parallel.

After the dashboard endpoint is merged, the only clearly safe parallel tasks are:
- dashboard API tests
- frontend dashboard service
- dashboard page shell

Do not parallelize tasks that heavily modify:
- `Dashboard.jsx`
- `Dashboard.css`
- `App.jsx`

## Human Review Areas

Require manual review for:
- login redirect changes
- route protection
- account access enforcement
- dashboard data correctness
- stale data when switching accounts
- any auth changes

## Transfer Verification Phase Guidance

The next phase is focused on defining and implementing transfer verification one step at a time.

- transfer verification must be user-approved in v1
- verified transfers must be visibly marked in the UI
- raw transactions must remain unchanged as source records
- prefer a separate transfer-link concept over changing transaction meaning directly
- do not implement multi-account totals yet in this phase
- avoid over-automation in candidate matching

For v1 transfer work:
- keep scope on candidate rules, verification, unverify behavior, transfer visibility, and transfer-link storage
- do not add auto-confirmation, advanced confidence logic, or broad matching workflows
- keep product behavior practical and reviewable from the existing React app
