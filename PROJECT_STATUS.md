# PROJECT_STATUS.md

## Current State

The project has moved past the original MVP queue and is ready for a dashboard-focused phase.

The current React UI still mainly consists of:
- Login
- UserInfo
- Import

`App.jsx` currently redirects authenticated users to `/import`, and login currently navigates to `/user-info`.

On the backend:
- auth and account routes are mapped in `Program.cs`
- controllers currently cover import and snapshot generation
- there is still no dashboard read endpoint
- snapshots are generated after import, but are not yet exposed through a read API for dashboard use

## Updated Immediate Goal

Create a useful post-login dashboard and make it the default destination after login.

The dashboard MVP should:
- redirect authenticated users to `/dashboard`
- show one selected account overview
- show current balance
- show last updated date
- show transaction count
- show a recent transactions preview
- handle no-account and no-transaction states clearly

## Dashboard MVP Direction

The safest dashboard MVP for the current repo is:

- one new account-scoped backend read endpoint
- one new React `/dashboard` page
- summary + recent transactions only
- transaction-derived data in v1
- no snapshot charting yet
- no multi-account aggregated dashboard yet
- no advanced analytics yet

## Key Constraints

- `BudgetTrackerApp/frontend` remains the primary product UI
- `BudgetTrackerApp.Web` remains out of scope unless explicitly requested
- dashboard data should come from transactions in v1, not snapshot read APIs
- implementation should stay incremental and coding-agent friendly

## Updated Risks

### Auth / Redirect Flow
Changing the default post-login destination may break current assumptions in `Login.jsx` and `App.jsx`.

### Account State
Dashboard behavior must be clear for:
- users with no accounts
- users with accounts but no transactions
- users switching accounts

### Dashboard Data Correctness
Summary and recent transactions must match the selected account and avoid stale data when switching.

### Parallel Work Risk
After the backend contract is in place, only a small number of tasks are safe to parallelize. Most dashboard tasks converge on:
- `frontend/src/pages/Dashboard.jsx`
- `frontend/src/pages/Dashboard.css`
- `frontend/src/App.jsx`