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

## Transfer Verification v1 Architecture

### Source of Truth and Relationship Model

Raw imported transactions should remain the source of truth. Transfer behavior should be modeled as a separate link or relationship between transactions, not as a meaning-changing boolean flag on the raw transaction records.

For v1, a verified transfer is a user-confirmed link between two opposite-signed transactions in different accounts that represent the same internal movement of money.

Recommended persisted shape for that relationship:
- use a dedicated entity such as `TransactionTransferLink`
- store transaction references plus verification metadata on the link
- keep transfer state on the link instead of rewriting the raw `Transaction` rows
- enforce different-account, opposite-sign, and one-active-link-per-transaction invariants

Recommended architectural direction:
- keep imported transaction rows unchanged as source records
- add a separate transfer-link concept to represent verification
- keep the link responsible for transfer state instead of rewriting transaction meaning directly

### Response and UI Surface

Transfer status should be surfaced in transaction responses so the existing product UI can show transfer state clearly. Verified transfers should remain visible in transaction lists and details, and the UI should make it obvious when a transaction is:
- a suggested transfer candidate
- a verified transfer

This keeps single-account ledgers truthful while preparing future combined views to treat verified transfers as internal movement instead of external income or expense.

### Explicitly Out of Scope for v1

Do not design Transfer Verification v1 around:
- partial transfers
- one-to-many matches
- currency conversion
- auto-confirmation
- advanced matching UI
