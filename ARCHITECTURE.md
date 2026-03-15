# ARCHITECTURE.md

## Current Frontend Direction

Use `BudgetTrackerApp/frontend` as the primary product UI.

Do not add dashboard work to `BudgetTrackerApp.Web` unless explicitly requested.

## Dashboard Phase Architecture

### Dashboard v1 Backend Shape
For the first dashboard version, use:
- one account-scoped read endpoint
- one compact dashboard DTO contract
- one dashboard service

Recommended endpoint:
- `GET /api/accounts/{accountId}/dashboard`

Recommended v1 response shape:
- `accountId`
- `accountName`
- `currentBalance`
- `lastUpdated`
- `transactionCount`
- `recentTransactions[]`
- `hasTransactions`

Do not include in v1:
- charts
- snapshot series
- category summaries
- all-account aggregation
- budget analytics

### Dashboard v1 Data Source
Use transactions directly for dashboard v1.

Reason:
- transactions already exist and are queryable
- snapshot generation exists, but snapshot read APIs are not yet part of the current surface
- transaction-derived summary is the lowest-risk path for a first dashboard

### Frontend Dashboard Shape
Dashboard v1 should add:
- `/dashboard` route
- `Dashboard.jsx`
- `Dashboard.css`
- `dashboardService.js`

The page should include:
- header
- account selector
- ledger hero section
- recent activity section
- state-aware empty/loading/error handling

### Redirect Behavior
Update the React app so that:
- successful login goes to `/dashboard`
- authenticated `/` goes to `/dashboard`

Keep `/user-info` available only if useful, but remove it from the main flow.

## Dashboard-Phase Implementation Principle

Prefer this order:
1. DTO contract
2. backend service
3. backend endpoint
4. backend tests
5. frontend service
6. dashboard page shell
7. redirect change
8. page data wiring
9. visual polish and state handling

## Dashboard-Phase Concurrency Guidance

Safe parallel work begins only after the dashboard endpoint exists.

Safe parallel tasks:
- backend dashboard tests
- frontend dashboard service
- dashboard page shell / route

Do not heavily parallelize later dashboard tasks because they converge on the same frontend files.