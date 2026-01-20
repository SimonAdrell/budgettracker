# BudgetTracker Documentation

This directory contains technical documentation for the BudgetTracker application.

## Database Documentation

### [database/ER_DIAGRAM.md](database/ER_DIAGRAM.md)
Complete Entity Relationship diagram for the budget tracker database with:
- Full Mermaid ER diagram
- Detailed table descriptions
- Excel import column mappings
- Design rationale and considerations
- Relationship documentation

### [database/SCHEMA_OVERVIEW.md](database/SCHEMA_OVERVIEW.md)
Quick reference guide featuring:
- ASCII art visual representation
- Key relationships summary
- Excel to database field mapping table
- Primary and foreign key reference
- Design highlights

## Overview

The budget tracker uses PostgreSQL with the following main entities:

1. **User** - ASP.NET Identity users (already implemented)
2. **Account** - Bank accounts that can be shared between users
3. **Transaction** - Individual bank transactions from Excel imports
4. **Category** - Hierarchical transaction categories
5. **BalanceSnapshot** - Periodic balance snapshots for graphs
6. **AccountUser** - Junction table for account sharing

## Excel Import Support

The database is designed to import Swedish bank transaction exports with columns:
- Bokföringsdatum (Booking date)
- Transaktionsdatum (Transaction date)
- Text (Description)
- Insättning/Uttag (Deposit/Withdrawal)
- Behållning (Balance)

For detailed mapping, see the [ER Diagram](database/ER_DIAGRAM.md#excel-import-mapping).
