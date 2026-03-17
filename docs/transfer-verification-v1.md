# Transfer Verification v1

## Overview

This document defines the product rules for Transfer Verification v1.

It is the canonical rules reference for follow-up transfer tasks that need to define:
- transfer-link persistence
- transfer candidate heuristics
- transfer visibility in the `BudgetTrackerApp/frontend` UI

This document is intentionally definition-first. It describes what transfer verification means in the product before implementation tasks decide storage shape, endpoint contracts, or matching heuristics.

## Product Goal

Transfer Verification v1 exists to let the user confirm when two transactions from different accounts represent the same internal movement of money.

The product goal is to:
- keep single-account ledgers truthful
- keep raw imported transactions intact as source records
- make verified transfers clearly visible in the UI
- prepare future combined-account views to avoid treating internal movement as new income or new expense

Transfer verification is user-approved in v1. The product does not automatically confirm transfers.

## Core Definitions

### Raw transaction

A raw transaction is the imported source record already stored for an account. Transfer verification does not rewrite, merge, delete, or hide the raw transaction row.

### Transfer candidate

A transfer candidate is a possible pair of transactions that the product presents for user review as a potential internal transfer.

A transfer candidate:
- is not yet verified
- does not change ledger meaning
- does not change account balances
- does not change account balance history
- does not change how the underlying transactions are stored

Candidate status is a review state only.

### Verified transfer

A verified transfer is a user-confirmed link between two opposite-signed transactions in different accounts that represent the same internal movement of money.

A verified transfer changes product interpretation for that pair, but it does not change the underlying transaction records themselves.

### Verification

Verification is the user action that confirms a candidate pair should be treated as one internal transfer relationship.

In v1, verification means:
- the pair is now treated as a verified transfer
- the relationship is explicit and reviewable in the product
- each transaction remains visible in its own account ledger
- each transaction remains part of its own account balance history

Verification does not mean:
- delete either transaction
- collapse the pair into one ledger row
- rewrite amount, description, date, or balance on either transaction
- remove the transactions from account-level history

### Unverify

Unverify is the user action that removes verified-transfer meaning from a previously verified pair.

In v1, unverify means:
- the pair is no longer treated as a verified transfer
- the transactions return to ordinary unlinked transaction behavior
- no ledger rows are deleted
- no account balance history is recalculated only because of unverify

If the same pair still satisfies future candidate-detection rules, it may later appear again as a candidate. That is candidate-generation behavior, not special unverify behavior.

## Candidate Rules

This document defines what a candidate means, not the full heuristic used to find one. The heuristic is deferred to Task T3.

For T1 purposes, a transfer candidate must be understood as:
- a possible internal transfer pair
- composed of two transactions from different accounts
- not yet user-verified
- still represented by two normal raw transactions

Until the user verifies the pair, both transactions continue to behave exactly like ordinary transactions in their respective account views and histories.

## Verification Rules

When the user verifies a transfer in v1:
- the verified relationship applies to exactly two transactions
- the two transactions remain in different accounts
- the two transactions must be interpreted as opposite sides of the same internal movement of money
- the product must surface that verified state clearly in the UI

Verification is a product-level relationship decision. It is not a transaction mutation flow.

Verification must be treated as reviewable and reversible by the user.

## Unverify Rules

When the user unverifies a transfer in v1:
- the verified relationship is removed
- the pair stops being treated as internal movement for verified-transfer purposes
- both transactions remain present and unchanged in their account ledgers
- both transactions remain present and unchanged in account balance history inputs

Unverify does not introduce a compensating transaction, a deletion, or a balance correction. It only removes the verified-link meaning.

## UI Visibility Requirements

Transfer Verification v1 must be visible in the product UI. Verified transfers cannot become hidden bookkeeping state.

The UI requirements for T1 are:
- a user must be able to tell when a transaction is part of a verified transfer
- verified transfers must remain visible in both account ledgers
- transfer visibility must preserve the fact that there are still two account-specific transactions
- the UI must distinguish unverified review state from verified transfer state

This document does not define the final badge copy, action layout, or review flow details. Those are deferred to Task T4.

## Ledger And Balance-History Invariants

The following rules must stay true in v1:

### Ledger invariants

- Verified transfers remain visible in both account ledgers.
- Verification does not remove, merge, or replace either ledger entry.
- Single-account ledger views remain truthful account histories after verification.

### Balance-history invariants

- Verified transfers remain part of each account's own balance history.
- Verification does not remove either transaction from account-level running balance continuity.
- Verification does not change the imported balance value stored on either transaction.
- Any account-specific balance history derived from those transactions, including snapshot generation based on transaction balances, remains unchanged by verification alone.

### Source-of-truth invariant

- Raw transactions remain the source records in v1.
- Transfer verification adds relationship meaning around raw transactions; it does not replace them as the source of truth.

## Combined-View Expectations

Future combined-account views should behave differently from single-account views.

For future combined views:
- a verified transfer should be treated as internal movement between owned accounts
- a verified transfer should not be treated as new external income
- a verified transfer should not be treated as new external expense

This rule applies to future combined-account interpretation only. It does not change how each individual account ledger or account balance history behaves in v1.

Transfer verification should therefore be treated as a prerequisite for reliable future combined-account totals, but combined-account totals are not implemented in this task.

## Explicit v1 Non-Goals Or Deferred Cases

Transfer Verification v1 does not define or implement:
- auto-confirmation
- advanced confidence scoring
- broad automated matching workflows
- currency conversion handling
- partial transfers
- one-to-many or many-to-one matching
- multi-account totals implementation
- hiding verified transfers from single-account ledgers
- rewriting raw transaction data to make a transfer "fit"

Follow-up tasks may define storage shape, matching heuristics, and concrete UI interactions, but they must stay within the product rules in this document.
