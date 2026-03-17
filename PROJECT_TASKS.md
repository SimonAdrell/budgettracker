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
| 16 | Move remaining minimal APIs to controllers | Sequential after 15 | 15 | `BudgetTrackerApp/BudgetTrackerApp.ApiService/Program.cs`, `BudgetTrackerApp/BudgetTrackerApp.ApiService/Controllers/` |

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

### [x] Task 2 — Dashboard query service
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

### [x] Task 3 — Dashboard endpoint
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

### [x] Task 15 — Final smoke-test pass
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

---

### [x] Task 16 — Move remaining minimal APIs to controllers
**Status:** Sequential after 15  
**Depends on:** 15  
**Goal:** Move application endpoints out of `Program.cs` and into controllers.  
**Why it matters:** Keeps API routing consistent and prevents new endpoint work from bypassing the controller/service pattern.  
**Likely files:**
- `BudgetTrackerApp/BudgetTrackerApp.ApiService/Program.cs`
- `BudgetTrackerApp/BudgetTrackerApp.ApiService/Controllers/`

**Implementation instructions:**
- Move remaining auth/account application endpoints from `Program.cs` into controller classes.
- Keep route shapes and authorization behavior unchanged.
- Leave infrastructure/bootstrap-only setup in `Program.cs`.

**Success criteria:**
- Application endpoints no longer live in `Program.cs`.
- Controller routes preserve current behavior.

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

## Next Planned Phase — Transfer Verification v1

### Summary

This is the next planned phase after the current dashboard work.

Transfer Verification v1 should define how the product:
- suggests possible internal transfer matches between accounts
- lets the user confirm or undo a transfer link
- keeps verified transfers visible in the UI and in each account ledger
- prepares future combined-account views to treat verified transfers as internal movement instead of external income or expense

This first queue is intentionally definition-first. It should document product rules, data-model direction, initial matching heuristics, and UI behavior before implementation tasks expand.

### Summary Table

| # | Task | Status | Depends On | Main Files |
|---|---|---|---|---|
| T1 | Transfer definition and rules doc | Sequential | None | `docs/transfer-verification-v1.md`, repo planning docs |
| T2 | Transfer data-model proposal | Sequential | T1 | `docs/transfer-verification-v1.md`, `ARCHITECTURE.md` |
| T3 | Transfer candidate heuristics spec | Sequential | T1, T2 | `docs/transfer-verification-v1.md` |
| T4 | UI behavior spec for transfer visibility | Sequential | T1, T3 | `docs/transfer-verification-v1.md` |
| T5 | First implementation planning pass | Sequential | T1-T4 | `PROJECT_TASKS.md`, optionally `docs/transfer-verification-v1.md` |

### Numbered Checklist

### [ ] Task T1 - Transfer definition and rules doc
**Status:** Sequential  
**Depends on:** None  
**Goal:** Write down the product rules for what a transfer is, what a candidate is, what verification means, and what changes after verification.  
**Why it matters:** Prevents implementation drift before code changes begin.  
**Likely files:**
- `docs/transfer-verification-v1.md`
- optionally cross-links from `PROJECT_STATUS.md`, `PROJECT_TASKS.md`, `ARCHITECTURE.md`, and `AGENT_GUIDE.md`

**Implementation instructions:**
- Document:
  - verified transfer definition
  - candidate definition
  - UI visibility requirement
  - unverify behavior
  - what remains unchanged in the ledger
  - what future combined views should do differently
- Keep the wording implementation-ready and avoid adding product behavior beyond the agreed v1 scope.

**Success criteria:**
- Transfer rules are documented clearly enough to implement from without guessing.

---

### [ ] Task T2 - Transfer data-model proposal
**Status:** Sequential  
**Depends on:** T1  
**Goal:** Define the proposed persistence shape for verified transfer links.  
**Why it matters:** Raw transactions should stay intact, and the relationship needs its own model.  
**Likely files:**
- `docs/transfer-verification-v1.md`
- optionally `ARCHITECTURE.md`

**Implementation instructions:**
- Propose a separate link entity such as `TransactionTransferLink` or equivalent.
- Document likely fields such as:
  - `id`
  - `fromTransactionId`
  - `toTransactionId`
  - `status`
  - `createdAt`
  - `verifiedAt`
  - `verifiedByUserId`
- Document basic invariants:
  - linked transactions must be in different accounts
  - linked transactions must have opposite signs
  - one active verified link per transaction

**Success criteria:**
- Model proposal is specific enough to guide implementation tasks.

---

### [ ] Task T3 - Transfer candidate heuristics spec
**Status:** Sequential  
**Depends on:** T1, T2  
**Goal:** Define the initial matching rules for possible transfer candidates.  
**Why it matters:** The app needs a predictable and reviewable way to suggest transfer matches.  
**Likely files:**
- `docs/transfer-verification-v1.md`

**Implementation instructions:**
- Define simple v1 heuristics:
  - different accounts
  - opposite signs
  - same amount
  - close date proximity
  - not already linked
  - not already part of another active candidate pair
- Explicitly avoid over-complicated matching in v1.

**Success criteria:**
- Heuristics are documented in a way that can be directly implemented and tested.

---

### [ ] Task T4 - UI behavior spec for transfer visibility
**Status:** Sequential  
**Depends on:** T1, T3  
**Goal:** Define how candidate and verified transfers appear in the UI.  
**Why it matters:** Visibility in the UI is a core product requirement.  
**Likely files:**
- `docs/transfer-verification-v1.md`

**Implementation instructions:**
- Specify:
  - verified transfer badge or label
  - candidate badge or label
  - confirm action
  - review linked counterpart transaction
  - undo or unverify action
  - minimal copy guidance
- Keep the UI practical for the existing React product.

**Success criteria:**
- UI requirements are clear enough to build without inventing new product behavior.

---

### [ ] Task T5 - First implementation planning pass
**Status:** Sequential  
**Depends on:** T1-T4  
**Goal:** Convert the documented transfer rules into the first implementation-ready coding-agent tasks.  
**Why it matters:** Turns the definition phase into actionable next steps.  
**Likely files:**
- `PROJECT_TASKS.md`
- optionally `docs/transfer-verification-v1.md`

**Implementation instructions:**
- Add a short follow-on note describing the likely first implementation tasks after the definition phase:
  - persistence/model task
  - migration task
  - backend candidate query task
  - verify/unverify endpoint task
  - transaction DTO/status task
  - frontend badge/review action task
- Do not fully expand these implementation tasks yet. Just frame the likely next phase.

**Success criteria:**
- The repo has a clear path from product definition to implementation planning.
