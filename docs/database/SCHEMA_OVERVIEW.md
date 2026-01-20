# Budget Tracker Database Schema - Quick Reference

## Entity Relationship Overview

This is a simplified visual representation of the database schema. For full details, see [ER_DIAGRAM.md](./ER_DIAGRAM.md).

```
┌─────────────────┐
│      User       │ (ASP.NET Identity - Already exists)
│─────────────────│
│ • Id (PK)       │
│ • UserName      │
│ • Email         │
│ • FirstName     │
│ • LastName      │
└────────┬────────┘
         │
         │ Many-to-Many via AccountUser
         │
         ▼
┌─────────────────┐         ┌──────────────────┐
│  AccountUser    │◄────────│     Account      │
│─────────────────│         │──────────────────│
│ • Id (PK)       │         │ • Id (PK)        │
│ • UserId (FK)   │         │ • Name           │
│ • AccountId(FK) │         │ • AccountNumber  │
│ • Role          │         │ • CreatedAt      │
│ • GrantedAt     │         │ • UpdatedAt      │
└─────────────────┘         └────────┬─────────┘
                                     │
                     ┌───────────────┼────────────────┐
                     │               │                │
                     ▼               ▼                ▼
            ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐
            │ Transaction  │  │  Category    │  │ BalanceSnapshot  │
            │──────────────│  │──────────────│  │──────────────────│
            │ • Id (PK)    │  │ • Id (PK)    │  │ • Id (PK)        │
            │ • AccountId  │◄─│ • Name       │  │ • AccountId (FK) │
            │   (FK)       │  │ • Description│  │ • SnapshotDate   │
            │ • CategoryId │  │ • Color      │  │ • Balance        │
            │   (FK)       │  │ • ParentId   │  │ • CreatedAt      │
            │              │  │   (FK-self)  │  └──────────────────┘
            │ Excel fields:│  │ • CreatedAt  │
            │──────────────│  └──────────────┘
            │ • BookingDate│         ▲
            │ • TransDate  │         │
            │ • Description│         │ Self-reference
            │ • Amount     │         │ (Parent/Child)
            │ • Balance    │         │
            │ • Original   │         ▼
            │ • ImportedAt │
            │ • CreatedAt  │
            └──────────────┘
```

## Key Relationships

1. **User ↔ Account** (Many-to-Many)
   - Via `AccountUser` junction table
   - Supports account sharing with role-based access

2. **Account → Transaction** (One-to-Many)
   - Each account has multiple transactions
   - Transactions store all Excel import data

3. **Category → Transaction** (One-to-Many)
   - Transactions can be categorized (optional)
   - Categories support hierarchy (parent-child)

4. **Account → BalanceSnapshot** (One-to-Many)
   - Periodic snapshots for efficient balance graphs
   - Complements transaction-level balance tracking

## Excel Import Mapping

| Excel Column      | Database Field             |
|-------------------|----------------------------|
| Bokföringsdatum   | Transaction.BookingDate    |
| Transaktionsdatum | Transaction.TransactionDate|
| Text              | Transaction.Description    |
| Insättning/Uttag  | Transaction.Amount         |
| Behållning        | Transaction.Balance        |

## Table Cardinalities

```
User (1) ────< AccountUser (M)
Account (1) ────< AccountUser (M)
Account (1) ────< Transaction (M)
Account (1) ────< BalanceSnapshot (M)
Category (1) ────< Transaction (M)
Category (1) ────< Category (M) [self-reference for hierarchy]
```

## Primary Keys & Foreign Keys

**Primary Keys:**
- All tables use auto-incrementing integer `Id` except User (GUID from Identity)

**Foreign Keys:**
- `AccountUser.UserId` → `User.Id`
- `AccountUser.AccountId` → `Account.Id`
- `Transaction.AccountId` → `Account.Id`
- `Transaction.CategoryId` → `Category.Id` (nullable)
- `Category.ParentCategoryId` → `Category.Id` (nullable, self-reference)
- `BalanceSnapshot.AccountId` → `Account.Id`

## Design Highlights

✓ **Excel Import Ready**: Transaction table maps directly to bank export format  
✓ **Multi-User Support**: Accounts can be shared via AccountUser  
✓ **Flexible Categorization**: Hierarchical categories with optional assignment  
✓ **Balance Tracking**: Dual approach (per-transaction + snapshots)  
✓ **Audit Trail**: CreatedAt, UpdatedAt, ImportedAt timestamps  
✓ **Extensible**: Room for budgets, recurring transactions, attachments  

For complete field descriptions and design rationale, see [ER_DIAGRAM.md](./ER_DIAGRAM.md).
