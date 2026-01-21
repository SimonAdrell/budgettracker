# Balance Snapshot Generation

## Overview

The Balance Snapshot feature provides a fast and efficient way to query and graph account balances over time. Instead of calculating balances from transactions on every query, the system pre-computes and stores daily balance snapshots.

## How It Works

### Data Model

The `BalanceSnapshot` table stores periodic (daily) balance snapshots for each account:

```csharp
public class BalanceSnapshot
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public DateOnly SnapshotDate { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
    public Account Account { get; set; }
}
```

Key constraints:
- Unique index on `(AccountId, SnapshotDate)` prevents duplicate snapshots
- Cascade delete ensures snapshots are removed when accounts are deleted

### Balance Calculation Logic

The `BalanceSnapshotService` uses the following approach to generate snapshots:

1. **Query Transactions**: Retrieves all transactions for an account up to and including the snapshot date
2. **Find Last Transaction**: For each day, finds the last transaction on or before that date
3. **Use Transaction Balance**: Uses the `Balance` field from that transaction
4. **Forward Fill**: If no transactions exist on a given day, uses the balance from the most recent prior transaction
5. **Upsert**: Creates new snapshots or updates existing ones if balances have changed

### Example

Given these transactions:
```
Jan 1: Balance = $1,000
Jan 3: Balance = $1,500
Jan 5: Balance = $1,300
```

Generated snapshots (Jan 1-5):
```
Jan 1: $1,000 (transaction on this day)
Jan 2: $1,000 (forward fill from Jan 1)
Jan 3: $1,500 (transaction on this day)
Jan 4: $1,500 (forward fill from Jan 3)
Jan 5: $1,300 (transaction on this day)
```

## API Endpoints

### Automatic Generation

Snapshots are automatically generated after successful transaction imports:

```http
POST /api/import/upload
Content-Type: multipart/form-data

file: [Excel file]
accountId: 1
```

The import endpoint will:
1. Import transactions
2. Automatically generate snapshots for the date range of imported transactions

### Manual Generation

#### Generate for Specific Account

```http
POST /api/snapshots/generate/{accountId}
Authorization: Bearer {token}
```

Response:
```json
{
  "message": "Generated 31 balance snapshots",
  "count": 31
}
```

#### Generate for All Accounts

```http
POST /api/snapshots/generate-all
Authorization: Bearer {token}
```

Response:
```json
{
  "message": "Generated 156 balance snapshots for 5 accounts",
  "count": 156,
  "accountCount": 5
}
```

## Service Methods

The `IBalanceSnapshotService` provides three main methods:

### GenerateSnapshotsAsync

Generates snapshots for a specific date range:

```csharp
Task<int> GenerateSnapshotsAsync(int accountId, DateOnly startDate, DateOnly endDate)
```

**Use Case**: Generate or update snapshots for a specific time period (e.g., after importing transactions for a specific month)

### GenerateSnapshotsForAllTransactionsAsync

Generates snapshots for the entire transaction history of an account:

```csharp
Task<int> GenerateSnapshotsForAllTransactionsAsync(int accountId)
```

**Use Case**: Initial population of snapshots or complete regeneration after data corrections

### RegenerateSnapshotsForAccountsAsync

Bulk regeneration for multiple accounts:

```csharp
Task<int> RegenerateSnapshotsForAccountsAsync(IEnumerable<int> accountIds)
```

**Use Case**: System maintenance, batch processing, or scheduled regeneration

## Performance Considerations

### Query Efficiency

The service uses a single query to fetch all transactions up to the end date, then processes them in memory:

```csharp
var transactions = await _context.Transactions
    .Where(t => t.AccountId == accountId && t.TransactionDate <= endDate)
    .OrderBy(t => t.TransactionDate)
    .ThenBy(t => t.Id)
    .Select(t => new { t.TransactionDate, t.Balance })
    .ToListAsync();
```

This approach:
- ✅ Minimizes database round trips
- ✅ Efficient for typical account sizes (thousands of transactions)
- ✅ Scales well with indexed `TransactionDate` column

### Upsert Pattern

The service uses an efficient upsert pattern to avoid conflicts:

```csharp
private async Task UpsertSnapshotAsync(int accountId, DateOnly snapshotDate, decimal balance)
{
    var existingSnapshot = await _context.BalanceSnapshots
        .FirstOrDefaultAsync(bs => bs.AccountId == accountId && bs.SnapshotDate == snapshotDate);

    if (existingSnapshot != null)
    {
        if (existingSnapshot.Balance != balance)
        {
            existingSnapshot.Balance = balance;
            existingSnapshot.CreatedAt = DateTime.UtcNow;
        }
    }
    else
    {
        _context.BalanceSnapshots.Add(new BalanceSnapshot { ... });
    }

    await _context.SaveChangesAsync();
}
```

**Note**: This saves changes for each snapshot. For large batch operations, consider batching the saves.

## Data Integrity

### Transaction Imports

When transactions are imported:
1. Transactions are saved to the database
2. Snapshots are automatically generated for the date range of imported transactions
3. Any existing snapshots in that range are updated with recalculated balances

### Transaction Edits

When transactions are edited or deleted:
- **Current Behavior**: Snapshots are not automatically updated
- **Recommended**: Call `GenerateSnapshotsForAllTransactionsAsync` to regenerate snapshots

**Future Enhancement**: Consider implementing automatic snapshot updates when transactions are modified.

### Data Correctness

The system ensures correctness by:
- Using the `Balance` field from transactions (sourced from bank exports)
- Always recalculating from scratch when regenerating
- Using the unique index to prevent duplicate snapshots
- Updating `CreatedAt` timestamp when balances are recalculated

## Usage Examples

### In Service Code

```csharp
public class ImportService
{
    private readonly IBalanceSnapshotService _snapshotService;

    public async Task<ImportResponse> ImportTransactionsFromExcelAsync(...)
    {
        // Import transactions...
        
        if (result.ImportedCount > 0)
        {
            // Automatically generate snapshots
            await _snapshotService.GenerateSnapshotsForAllTransactionsAsync(accountId);
        }
        
        return result;
    }
}
```

### In API Endpoints

```csharp
app.MapPost("/api/snapshots/generate/{accountId}", async (
    int accountId,
    HttpContext httpContext,
    IAccountService accountService,
    IBalanceSnapshotService snapshotService) =>
{
    // Verify access...
    
    var count = await snapshotService.GenerateSnapshotsForAllTransactionsAsync(accountId);
    return Results.Ok(new { message = $"Generated {count} balance snapshots", count });
});
```

## Future Enhancements

Potential improvements to consider:

1. **Scheduled Background Jobs**: Use a background service (e.g., Hangfire, Quartz.NET) to regenerate snapshots nightly
2. **Configurable Intervals**: Support weekly or monthly snapshots in addition to daily
3. **Automatic Updates**: Hook into transaction save/update/delete events to automatically update affected snapshots
4. **Batch Processing**: Optimize large regeneration operations by batching database saves
5. **Incremental Updates**: Only regenerate snapshots for dates affected by transaction changes
6. **Snapshot Retention**: Implement policies to archive or delete old snapshots (e.g., keep daily for 1 year, then monthly)

## Testing

Comprehensive unit tests are provided in `BalanceSnapshotServiceTests.cs`:

- ✅ Snapshot generation with no transactions
- ✅ Snapshot generation with single transaction
- ✅ Snapshot generation with multiple transactions across dates
- ✅ Multiple transactions on the same day (uses last balance)
- ✅ Updating existing snapshots
- ✅ Invalid date range handling
- ✅ Bulk regeneration for multiple accounts

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~BalanceSnapshotServiceTests"
```

## Troubleshooting

### Snapshots Not Updating After Import

**Symptom**: Old balance values remain after importing new transactions

**Solution**: Manually trigger regeneration:
```bash
POST /api/snapshots/generate/{accountId}
```

### Performance Issues with Large Accounts

**Symptom**: Slow snapshot generation for accounts with many transactions

**Potential Solutions**:
1. Consider using date range parameters to limit snapshot generation
2. Implement batch processing for `SaveChangesAsync`
3. Use background jobs for large regeneration operations

### Duplicate Key Violations

**Symptom**: `Unique constraint violation on (AccountId, SnapshotDate)`

**Cause**: Concurrent snapshot generation for the same account

**Solution**: The service uses an upsert pattern that should handle this, but ensure snapshot generation is not called concurrently for the same account.

## References

- [ER Diagram](../database/ER_DIAGRAM.md) - Database schema and relationships
- [Schema Overview](../database/SCHEMA_OVERVIEW.md) - Quick reference for the database schema
- [Balance Snapshot Entity](../../BudgetTrackerApp/BudgetTrackerApp.ApiService/Models/BalanceSnapshot.cs)
- [Balance Snapshot Service](../../BudgetTrackerApp/BudgetTrackerApp.ApiService/Services/BalanceSnapshotService.cs)
