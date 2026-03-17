# Task 15 Final Smoke Checklist

Executed on 2026-03-16 against `BudgetTrackerApp/frontend` using the live AppHost stack.

- [x] Unauthenticated `/dashboard` redirects to `/login`
- [x] Login lands on `/dashboard`
- [x] Authenticated `/` redirects to `/dashboard`
- [x] No-account state renders for a new user
- [x] No-transaction state renders for an empty account
- [x] Populated account state renders balance summary and recent transactions
- [x] Account switching clears stale data and loads the selected account
- [x] Import flow returns to the dashboard and shows the newly imported account state

Verification notes:
- `dotnet test BudgetTrackerApp/BudgetTrackerApp.Tests/BudgetTrackerApp.Tests.csproj --filter DashboardTests`
- `npm ci`
- `npm run lint`
- `npm run build`
- Browser-driven smoke pass against the React frontend resource started by Aspire

Regression watch:
- No auth redirect regressions observed
- No import workflow regressions observed
- No account selection or stale-data regressions observed
- No balance or date-semantic regressions observed in the smoke scenarios above
