# Project Status

## Current Baseline

This repository is an early-stage but real budget-tracker codebase with solid backend foundations and working auth/import flows, but it still lacks the main "read/use your data" product layer.

At the repo root, the main solution lives under `BudgetTrackerApp/`, with supporting docs in `README.md`, `API_REFERENCE.md`, `IDENTITY_SETUP.md`, and `docs/`.

Inside `BudgetTrackerApp/`, the solution currently includes:

- `BudgetTrackerApp.AppHost`
- `BudgetTrackerApp.ApiService`
- `BudgetTrackerApp.ServiceDefaults`
- `BudgetTrackerApp.Web`
- `frontend`
- `BudgetTrackerApp.Tests`

The stack is a mixed .NET + React setup:

- Aspire-based local orchestration in `BudgetTrackerApp.AppHost`
- ASP.NET Core API with PostgreSQL, ASP.NET Core Identity, JWT auth, and refresh tokens
- React 19 + Vite frontend in `BudgetTrackerApp/frontend`
- Blazor frontend in `BudgetTrackerApp.Web`

## Product UI Scope

- `BudgetTrackerApp/frontend` is the primary MVP UI and the default target for product-surface work.
- `BudgetTrackerApp.Web` is out of scope unless a task explicitly names it.
- Agents should not split MVP UI work across both frontends.

## What Already Exists

- Authentication is real, not stubbed: register, login, refresh, and logout are implemented and tested.
- The frontend already supports login/logout behavior and stores auth state in `localStorage`.
- Basic account management exists: list current-user accounts, fetch by ID, create an account, and create the owner link.
- Excel import is implemented end to end: upload, validate, parse, skip duplicates, persist transactions, and regenerate balance snapshots.
- The React import page already supports account selection, inline account creation, `.xls` / `.xlsx` upload, and success/warning/error messaging.
- Balance snapshots are implemented on the backend, including generation endpoints and substantial unit coverage.
- Automated testing already covers snapshots, identity flows, import behavior, and a web smoke test.

## What Is Only Partially Implemented

- The React app is still very small: `/`, `/login`, `/user-info`, and `/import` are the main visible routes.
- There is no visible transaction-history page, dashboard, reporting page, category-management UI, or registration page.
- The backend model is ahead of the exposed UI/API surface: the schema includes accounts, account-user links, categories, transactions, balance snapshots, and refresh tokens.
- The repo contains two frontends, but only the React app currently looks product-oriented.
- Documentation trails the code: the API docs do not cover all real endpoints, and the frontend README is still the stock Vite template.

## What Is Missing For A Usable MVP

The biggest missing area is the read/query side of the product.

- There is no visible transaction-read API for browsing imported data.
- There is no transaction list UI in the React app.
- Snapshot generation exists, but there is no clear read endpoint for balance summaries or history charts.
- Registration is supported by the backend but not surfaced in the React UI.

Also missing or incomplete:

- Category assignment flows
- Budget logic
- Dashboard and reporting views
- Richer account-management UX
- Multiple importer implementations

## Inferred Product Goal

This project appears to target a personal finance / budget tracker where an authenticated user:

- creates one or more accounts
- imports bank-export spreadsheets
- stores transactions
- derives balance snapshots for reporting, graphing, or budgeting

The backend already contains enough structure to support that path incrementally. The missing work is mostly product-surface work rather than a full backend rewrite.

## MVP Definition

The smallest useful MVP for this codebase is:

- user can register, log in, and log out
- user can create at least one account
- user can import a supported bank Excel file into that account
- imported transactions are saved with duplicate skipping
- user can view imported transactions for an account
- user can see a simple balance summary derived from the latest transaction or snapshots

The MVP should be "import and view your bank data safely," not a full personal-finance platform.

## Intentionally Deferred

These can wait until after the MVP flow works:

- category hierarchy and category-management UX
- budget rules and monthly planning
- charts and richer analytics
- account-sharing UI
- multiple importer plugins
- advanced reporting/export
- production-grade observability and deployment polish

## Key Risks

- Dual-frontend ambiguity: agents may accidentally build in `BudgetTrackerApp.Web` instead of `BudgetTrackerApp/frontend`.
- Financial correctness: duplicate detection, booking date vs. transaction date, and balance calculations are easy to break with "small" changes.
- Auth drift: the backend supports refresh tokens, but the frontend does not yet appear to implement refresh/retry behavior.
- Structural cleanup debt: misnamed `.cs.cs` files, stale weather/sample docs, and route ambiguity in the frontend service layer can slow agent work.
