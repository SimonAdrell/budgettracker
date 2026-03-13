# Project Tasks

## Working Assumptions

- `BudgetTrackerApp/frontend` is the active MVP product UI.
- `BudgetTrackerApp.Web` is out of scope unless explicitly requested.
- Use one task per PR and one agent run per task.
- Prefer backend-first, then frontend, then reliability hardening.
- Do not batch unrelated work.

## Execution Phases

### Phase 1: Stabilize Existing Code

Purpose: remove ambiguity, align docs with reality, clean up obvious naming issues, and add smoke coverage for the flows that already work.

### Phase 2: Complete The MVP Flow

Purpose: finish the smallest real user journey by exposing registration, transaction reads, and a simple account summary in the React app.

### Phase 3: Improve Reliability And Structure

Purpose: reduce breakage during iterative agent work with a shared API client, refresh-token handling, and stronger import/snapshot regression tests.

### Phase 4: Post-MVP Features

Purpose: add categories, a better dashboard, and richer reporting only after the core flow is stable.

## Prioritized Execution Queue

### Stabilization Queue

1. Clarify product UI scope in `README.md`.
   Success: the docs clearly state that `BudgetTrackerApp/frontend` is the primary MVP UI and `BudgetTrackerApp.Web` is out of scope unless explicitly requested.
2. Rename `ServiceResponse.cs.cs` and `ServiceGuard.cs.cs` to normal `.cs` filenames.
   Success: no `.cs.cs` files remain and the solution still builds.
3. Add a backend smoke test for login -> create account.
   Success: one happy-path test proves auth plus account creation still works.
4. Add a shared `frontend/src/services/apiClient.js`.
   Success: there is one reusable frontend client with a single base-URL strategy.
5. Migrate `authService.js` to the shared API client.
   Success: auth calls no longer hardcode their own client wiring.
6. Migrate `accountService.js` to the shared API client.
   Success: account calls use the same shared client pattern.
7. Migrate `importService.js` to the shared API client.
   Success: all current frontend services use the shared client consistently.

### MVP Completion Queue

8. Add `frontend/src/pages/Register.jsx`.
   Success: users can submit registration from the browser.
9. Wire the `/register` route in `frontend/src/App.jsx`.
   Success: registration is reachable from the app.
10. Add a transaction list DTO in `BudgetTrackerApp.ApiService/DTOs/`.
   Success: the backend has a stable response model for transaction reads.
11. Add a transaction query method in the backend service layer.
   Success: transactions can be read for one accessible account, scoped to the current user.
12. Expose an authenticated transactions read endpoint.
   Success: valid users can fetch account transactions and invalid access is blocked.
13. Add a backend test for the transactions endpoint.
   Success: happy-path and access-control behavior are covered.
14. Add `frontend/src/services/transactionService.js`.
   Success: the React app has a thin client wrapper for transaction reads.
15. Add a `Transactions.jsx` page shell and route.
   Success: `/transactions` renders with heading, loading, and error states.
16. Render an account selector and transaction table.
   Success: a user can choose an account and see imported rows.
17. Add an account summary DTO and backend service method.
   Success: the backend can return a compact summary model for one account.
18. Expose an authenticated account summary endpoint.
   Success: the frontend can request current balance, last updated date, and transaction count.
19. Add a backend test for the summary endpoint.
   Success: summary contract and access rules are covered.
20. Render a summary card on the transactions page.
   Success: users see simple balance value above raw transaction data.

### Reliability Queue

21. Add refresh-token retry support to `apiClient.js`.
   Success: one expired access token can refresh and retry once.
22. Add logout fallback on refresh failure.
   Success: failed refresh clears auth state cleanly.
23. Remove temporary legacy `/api/api` compatibility support from the React frontend.
   Success: `BudgetTrackerApp/frontend/vite.config.js`, `frontend/src/services/apiClient.js`, and frontend docs no longer rely on or describe the temporary `/api/api/*` compatibility path, and the React app still builds.
24. Add duplicate/validation regression tests for import.
   Success: duplicate skipping and invalid inputs are automatically checked.
25. Add a snapshot-range regression test.
   Success: desired narrower regeneration behavior is defined in tests.
26. Narrow snapshot regeneration scope after import.
   Success: only the affected range is regenerated unless a full rebuild is required.

### Post-MVP Queue

27. Add a category list endpoint.
28. Add a category create endpoint.
29. Add a dashboard page shell in the React app.
30. Render account summary data on the dashboard page.

## Recommended Immediate Order

The best first tasks to run are:

1. UI scope clarification in docs
2. obvious filename cleanup
3. current happy-path smoke test
4. shared frontend API client
5. `authService` migration to the shared client

## Execution Rules For Agents

- Treat tasks 1-7 as stabilization, 8-20 as MVP completion, 21-25 as reliability hardening, and 26-29 as post-MVP work.
- Merge only after the task's own success criteria are met.
- Pair each backend change with a test or a reproducible API verification step.
- Pair each frontend task with `npm run build` and a short manual smoke-check note.
- Prefer additive changes over broad refactors.
