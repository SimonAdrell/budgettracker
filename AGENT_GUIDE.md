# Agent Guide

## Target Surface

- The default product UI target is `BudgetTrackerApp/frontend`.
- `BudgetTrackerApp.Web` is out of scope unless a task explicitly names it.
- The repo should move toward one clear MVP user flow in React, not duplicate feature work across both frontends.

## Core Working Rules

- Make minimal changes.
- Work on one task at a time.
- Keep each PR or agent run focused on one vertical slice.
- Prefer additive changes over structural churn.
- Do not modify application code unless the task requires it.
- Do not refactor unrelated areas while implementing a feature.
- Do not invent missing product behavior; verify what already exists first.

## Scope Discipline

- Start from the current MVP goal: import and view bank data safely.
- Extend what already exists before proposing new architecture.
- Treat the backend model as a source of hints, not proof that a full feature is already implemented.
- If a task is ambiguous, restate the concrete target surface before making changes.

## Preferred Task Flow

1. Confirm the task scope and the correct target project.
2. Inspect the existing code path before changing anything.
3. Change the smallest useful set of files.
4. Add or update tests when backend behavior changes.
5. Verify the exact success path described in the task.
6. Stop after the requested slice is complete.

## Verification Expectations

- Backend work: add or update automated tests, or provide a reproducible API verification path.
- Frontend work: run `npm run build` and do a short manual smoke check of the affected path.
- Import/snapshot work: verify financial behavior, not just HTTP or rendering behavior.
- Auth work: verify login state, protected access, and failure behavior.

## Human-Review Areas

Human review should be mandatory for:

- auth and security changes
- token storage, refresh, logout, and CORS behavior
- EF Core model changes and migrations
- startup migration behavior
- import parsing logic
- duplicate-detection rules
- snapshot math and date semantics
- any task that touches account-access rules or sharing behavior

## Safe Defaults For Agents

- Prefer backend-first, then frontend.
- Prefer one endpoint, one page, one service, or one test addition per task.
- Reuse existing patterns in `frontend/src/pages`, `frontend/src/services`, `BudgetTrackerApp.ApiService`, and `BudgetTrackerApp.Tests`.
- Keep docs aligned with the code whenever the exposed behavior changes.

## What Not To Do

- Do not build features in both frontends.
- Do not batch dashboard, categories, auth cleanup, and API refactors into one change.
- Do not "simplify" financial date logic without tests proving the result is still correct.
- Do not broaden scope because the schema suggests future features.
- Do not rename files or move modules unless the task explicitly calls for it.
