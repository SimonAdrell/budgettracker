# PROJECT_TASKS.md

## Completed MVP Tasks

The original MVP queue is considered completed except for earlier post-MVP items, which are now superseded by the dashboard phase.

## Current Goal

Build a useful post-login dashboard and make it the default destination after login.

## Dashboard MVP

The first dashboard version should:
- redirect authenticated users to `/dashboard`
- show one selected account
- show current balance prominently
- show last updated date
- show transaction count
- show recent transactions preview
- support no-account and no-transaction states
- provide a clear path to import transactions

## Dashboard Phase Summary

| # | Task | Status | Depends On | Main Files |
|---|---|---|---|---|
| 1 | Dashboard DTO contract | Sequential | — | `BudgetTrackerApp/BudgetTrackerApp.ApiService/DTOs/DashboardDTOs.cs` |
| 2 | Dashboard query service | Sequential | 1 | `BudgetTrackerApp/BudgetTrackerApp.ApiService/Services/DashboardService.cs`, `BudgetTrackerApp/BudgetTrackerApp.ApiService/Services/ServiceRegistration.cs` |
| 3 | Dashboard endpoint | Sequential | 2 | `BudgetTrackerApp/BudgetTrackerApp.ApiService/Program.cs` |
| 4 | Dashboard API tests | Parallel after 3 | 3 | `BudgetTrackerApp/BudgetTrackerApp.Tests/DashboardTests.cs` |
| 5 | Frontend dashboard service client | Parallel after 3 | 3 | `BudgetTrackerApp/frontend/src/services/dashboardService.js` |
| 6 | Dashboard page shell and protected route | Parallel after 3 | 3 | `BudgetTrackerApp/frontend/src/pages/Dashboard.jsx`, `BudgetTrackerApp/frontend/src/pages/Dashboard.css`, `BudgetTrackerApp/frontend/src/App.jsx` |
| 7 | Post-login redirect update | Sequential | 6 | `BudgetTrackerApp/frontend/src/pages/Login.jsx`, `BudgetTrackerApp/frontend/src/App.jsx`, optionally `BudgetTrackerApp/frontend/src/pages/UserInfo.jsx` |
| 8 | Account loading and default selection | Sequential | 5, 6 | `BudgetTrackerApp/frontend/src/pages/Dashboard.jsx` |
| 9 | Dashboard data fetch wiring | Sequential | 5, 8 | `BudgetTrackerApp/frontend/src/pages/Dashboard.jsx` |
| 10 | Ledger hero section | Sequential | 9 | `BudgetTrackerApp/frontend/src/pages/Dashboard.jsx`, `BudgetTrackerApp/frontend/src/pages/Dashboard.css` |
| 11 | Recent transactions preview | Sequential | 10 | `BudgetTrackerApp/frontend/src/pages/Dashboard.jsx`, `BudgetTrackerApp/frontend/src/pages/Dashboard.css` |
| 12 | State-aware empty and first-run panels | Sequential | 8, 9 | `BudgetTrackerApp/frontend/src/pages/Dashboard.jsx`, `BudgetTrackerApp/frontend/src/pages/Dashboard.css` |
| 13 | Loading and error states | Sequential | 9 | `BudgetTrackerApp/frontend/src/pages/Dashboard.jsx`, `BudgetTrackerApp/frontend/src/pages/Dashboard.css` |
| 14 | Dashboard/import navigation polish | Sequential | 10, 11, 12, 13 | `BudgetTrackerApp/frontend/src/pages/Dashboard.jsx`, `BudgetTrackerApp/frontend/src/pages/Import.jsx`, optionally `BudgetTrackerApp/frontend/src/pages/UserInfo.jsx` |
| 15 | Final smoke-test pass | Sequential | 1–14 | optional checklist in `docs/` or PR checklist only |

## Dashboard Phase Checklist

### [x] Task 1 — Dashboard DTO contract
**Status:** Sequential  
**Depends on:** —  
**Goal:** Add the response shape for the dashboard MVP.  
**Why it matters:** Defines the API contract early and reduces guessing for both backend and frontend.  
**Likely files:**
- `BudgetTrackerApp/BudgetTrackerApp.ApiService/DTOs/DashboardDTOs.cs`

**Implementation instructions:**
- Create a minimal DTO set for:
  - selected account summary
  - recent transactions preview
  - empty-state flags
- Keep v1 fields limited to:
  - `accountId`
  - `accountName`
  - `currentBalance`
  - `lastUpdated`
  - `transactionCount`
  - `recentTransactions[]`
  - `hasTransactions`
- Do not add:
  - chart fields
  - snapshot-series fields
  - category fields
  - all-accounts fields

**Success criteria:**
- DTO file compiles cleanly.
- Contract is small enough that both backend and frontend can implement against it without follow-up changes.

---

### [ ] Task 2 — Dashboard query service
**Status:** Sequential  
**Depends on:** 1  
**Goal:** Implement the backend read logic for one selected account.  
**Why it matters:** Creates the data source for the dashboard without overexpanding scope.  
**Likely files:**
- `BudgetTrackerApp/BudgetTrackerApp.ApiService/Services/DashboardService.cs`
- `BudgetTrackerApp/BudgetTrackerApp.ApiService/Services/ServiceRegistration.cs`

**Implementation instructions:**
- Add `IDashboardService` with one method like `GetAccountDashboardAsync(accountId, ct)`.
- Query Transactions directly for:
  - latest transaction balance
  - latest transaction date
  - transaction count
  - top 6–8 recent transactions
- Reuse existing account access validation patterns.
- Return an explicit empty-account/no-transactions payload instead of throwing for no data.
- Do not read from snapshots in v1.

**Success criteria:**
- Service returns a populated DTO for accounts with transactions.
- Service returns a valid empty-state DTO for accounts with zero transactions.
- Service enforces account access.

---

### [ ] Task 3 — Dashboard endpoint
**Status:** Sequential  
**Depends on:** 2  
**Goal:** Expose one backend endpoint for the dashboard page.  
**Why it matters:** Makes the dashboard data available to the React app in one request.  
**Likely files:**
- `BudgetTrackerApp/BudgetTrackerApp.ApiService/Program.cs`

**Implementation instructions:**
- Add `GET /api/accounts/{accountId}/dashboard`.
- Keep it as a minimal API in `Program.cs` to match current auth/account endpoint style.
- Require authorization.
- Return:
  - `401` if unauthenticated
  - consistent auth behavior for account access violations
  - `200` with empty-state payload for valid empty accounts

**Success criteria:**
- Endpoint returns the new DTO.
- Endpoint is auth-protected.
- Endpoint works for both populated and empty accounts.

---

### [ ] Task 4 — Dashboard API tests
**Status:** Parallel after 3  
**Depends on:** 3  
**Goal:** Lock the backend contract before frontend integration.  
**Why it matters:** Prevents regressions and clarifies expected behavior early.  
**Likely files:**
- `BudgetTrackerApp/BudgetTrackerApp.Tests/DashboardTests.cs`

**Implementation instructions:**
- Add tests for:
  - unauthorized request
  - authenticated request with empty account
  - authenticated request with seeded/imported transactions
- Follow the integration-test pattern used in `ImportTests.cs`.

**Success criteria:**
- New tests pass locally.
- At least one happy-path and one auth-path test exist.

---

### [ ] Task 5 — Frontend dashboard service client
**Status:** Parallel after 3  
**Depends on:** 3  
**Goal:** Add the smallest frontend wrapper for the new endpoint.  
**Why it matters:** Gives the dashboard page a stable API call layer.  
**Likely files:**
- `BudgetTrackerApp/frontend/src/services/dashboardService.js`

**Implementation instructions:**
- Add `getAccountDashboard(accountId)`.
- Use the same token/header pattern as existing frontend services.
- Use the same `/api/api/...` path convention the current frontend already uses.

**Success criteria:**
- Service returns parsed dashboard JSON.
- API errors propagate without swallowing details.

---

### [ ] Task 6 — Dashboard page shell and protected route
**Status:** Parallel after 3  
**Depends on:** 3  
**Goal:** Create the page shell before wiring data.  
**Why it matters:** Establishes the new destination and page structure safely.  
**Likely files:**
- `BudgetTrackerApp/frontend/src/pages/Dashboard.jsx`
- `BudgetTrackerApp/frontend/src/pages/Dashboard.css`
- `BudgetTrackerApp/frontend/src/App.jsx`

**Implementation instructions:**
- Add a protected `/dashboard` route.
- Add a basic page shell with placeholder sections:
  - header
  - account selector area
  - ledger hero area
  - recent activity area
- Do not change redirects yet.
- Keep implementation inside `BudgetTrackerApp/frontend`.

**Success criteria:**
- Authenticated users can manually visit `/dashboard`.
- Unauthenticated users are still redirected to `/login`.

---

### [ ] Task 7 — Post-login redirect update
**Status:** Sequential  
**Depends on:** 6  
**Goal:** Make dashboard the default landing page.  
**Why it matters:** Moves the product flow to the new dashboard experience.  
**Likely files:**
- `BudgetTrackerApp/frontend/src/pages/Login.jsx`
- `BudgetTrackerApp/frontend/src/App.jsx`
- optionally `BudgetTrackerApp/frontend/src/pages/UserInfo.jsx`

**Implementation instructions:**
- Change login success navigation from `/user-info` to `/dashboard`.
- Change authenticated `/` redirect from `/import` to `/dashboard`.
- Keep `/user-info` available if needed, but remove it from the main flow.
- Do not change logout behavior.

**Success criteria:**
- Fresh login lands on `/dashboard`.
- Visiting `/` while authenticated lands on `/dashboard`.
- Existing login/logout still works.

---

### [ ] Task 8 — Account loading and default selection
**Status:** Sequential  
**Depends on:** 5, 6  
**Goal:** Load the user’s accounts and establish dashboard state.  
**Why it matters:** The dashboard cannot function until it knows which account is selected.  
**Likely files:**
- `BudgetTrackerApp/frontend/src/pages/Dashboard.jsx`

**Implementation instructions:**
- Fetch accounts on mount using existing `accountService`.
- If accounts exist, auto-select the first one.
- If no accounts exist, enter first-run state.
- Keep account selection as local state only for now.

**Success criteria:**
- Account dropdown renders for users with accounts.
- Zero-account users see a first-run state instead of a broken dashboard.

---

### [ ] Task 9 — Dashboard data fetch wiring
**Status:** Sequential  
**Depends on:** 5, 8  
**Goal:** Fetch dashboard data for the selected account.  
**Why it matters:** Connects the page to real account-specific data.  
**Likely files:**
- `BudgetTrackerApp/frontend/src/pages/Dashboard.jsx`

**Implementation instructions:**
- Fetch the dashboard payload whenever `selectedAccountId` changes.
- Clear stale data when switching accounts.
- Keep fetch logic inside the page for v1.
- Do not introduce global state.

**Success criteria:**
- Selecting a different account triggers exactly one refetch.
- Stale account data does not remain onscreen after account change.

---

### [ ] Task 10 — Ledger hero section
**Status:** Sequential  
**Depends on:** 9  
**Goal:** Render the summary-first part of the dashboard.  
**Why it matters:** Gives the user immediate high-value financial context.  
**Likely files:**
- `BudgetTrackerApp/frontend/src/pages/Dashboard.jsx`
- `BudgetTrackerApp/frontend/src/pages/Dashboard.css`

**Implementation instructions:**
- Render:
  - current balance as the dominant element
  - last updated date beneath it
  - transaction count as secondary metadata
- Keep layout calm and sparse.
- No chart, no KPI card grid, no secondary metrics row.

**Success criteria:**
- The main balance is visually dominant.
- Summary data is readable at a glance without scrolling.

---

### [ ] Task 11 — Recent transactions preview
**Status:** Sequential  
**Depends on:** 10  
**Goal:** Add the “proof that the data is real” section.  
**Why it matters:** Makes the dashboard feel grounded and trustworthy.  
**Likely files:**
- `BudgetTrackerApp/frontend/src/pages/Dashboard.jsx`
- `BudgetTrackerApp/frontend/src/pages/Dashboard.css`

**Implementation instructions:**
- Render top 6–8 recent transactions.
- Show only v1 fields:
  - date
  - description
  - amount
  - optional running balance if returned
- No filtering, sorting controls, search, or pagination.

**Success criteria:**
- Populated accounts show recent rows cleanly.
- The list does not overwhelm the summary hero.

---

### [ ] Task 12 — State-aware empty and first-run panels
**Status:** Sequential  
**Depends on:** 8, 9  
**Goal:** Add the guided empty states.  
**Why it matters:** Ensures the dashboard is useful even before data exists.  
**Likely files:**
- `BudgetTrackerApp/frontend/src/pages/Dashboard.jsx`
- `BudgetTrackerApp/frontend/src/pages/Dashboard.css`

**Implementation instructions:**
- Add two distinct empty states:
  - no accounts yet → primary CTA to create/import via the existing import flow
  - account exists but no transactions → primary CTA to import transactions
- Use one primary CTA only.
- Keep copy factual and short.

**Success criteria:**
- Empty states are clearly different.
- A user always has one obvious next action.

---

### [ ] Task 13 — Loading and error states
**Status:** Sequential  
**Depends on:** 9  
**Goal:** Make the dashboard feel trustworthy during transitions and failures.  
**Why it matters:** Avoids a blank or confusing experience while data is loading.  
**Likely files:**
- `BudgetTrackerApp/frontend/src/pages/Dashboard.jsx`
- `BudgetTrackerApp/frontend/src/pages/Dashboard.css`

**Implementation instructions:**
- Add:
  - loading placeholder or calm skeleton
  - fetch error state with retry
  - stable layout during transitions
- Do not use toast spam in v1.

**Success criteria:**
- Dashboard never appears blank while data is loading.
- API errors are recoverable from the page.

---

### [ ] Task 14 — Dashboard/import navigation polish
**Status:** Sequential  
**Depends on:** 10, 11, 12, 13  
**Goal:** Make the MVP feel like one connected workflow.  
**Why it matters:** Ties the dashboard and import experience together without adding a large navigation system.  
**Likely files:**
- `BudgetTrackerApp/frontend/src/pages/Dashboard.jsx`
- `BudgetTrackerApp/frontend/src/pages/Import.jsx`
- optionally `BudgetTrackerApp/frontend/src/pages/UserInfo.jsx`

**Implementation instructions:**
- Add “Import transactions” CTA on dashboard.
- After successful import, add a clear path back to dashboard.
- Keep navigation minimal; do not add a global sidebar/nav system yet.
- Reuse the existing import page rather than redesigning it.

**Success criteria:**
- User can move dashboard → import → dashboard without dead ends.
- Import remains functional.

---

### [ ] Task 15 — Final smoke-test pass
**Status:** Sequential  
**Depends on:** 1–14  
**Goal:** Catch regressions before expanding scope.  
**Why it matters:** Confirms the new dashboard flow did not break auth/import/account behavior.  
**Likely files:**
- optional checklist in `docs/`
- or PR checklist only

**Implementation instructions:**
Verify:
- unauthenticated `/dashboard` redirects to `/login`
- login lands on dashboard
- `/` lands on dashboard when authenticated
- no-account state
- no-transaction state
- populated account state
- account switching
- import then revisit dashboard

Do not refactor during this step.

**Success criteria:**
- Smoke checklist is complete.
- No auth/import/account creation regressions are introduced.

## Safe Parallel Work

Only a small part of the dashboard phase is safe to parallelize.

After **Task 3** is merged, these tasks can run in parallel:
- **Task 4** — Dashboard API tests
- **Task 5** — Frontend dashboard service client
- **Task 6** — Dashboard page shell and protected route

Do not parallelize most later dashboard tasks, because they converge on:
- `frontend/src/pages/Dashboard.jsx`
- `frontend/src/pages/Dashboard.css`
- `frontend/src/App.jsx`

## Recommended Starting Point

Start with **Task 1 — Dashboard DTO contract**.

Why:
- smallest high-leverage task
- locks the response shape early
- reduces ambiguity for both backend and frontend
- enables the backend and frontend dashboard work to proceed without guessing
