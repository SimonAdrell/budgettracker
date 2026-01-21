using BudgetTrackerApp.ApiService.Data;
using BudgetTrackerApp.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetTrackerApp.ApiService.Services;

public class BalanceSnapshotService : IBalanceSnapshotService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BalanceSnapshotService> _logger;

    public BalanceSnapshotService(ApplicationDbContext context, ILogger<BalanceSnapshotService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> GenerateSnapshotsAsync(int accountId, DateOnly startDate, DateOnly endDate)
    {
        if (startDate > endDate)
        {
            throw new ArgumentException("Start date must be before or equal to end date");
        }

        _logger.LogInformation("Generating balance snapshots for account {AccountId} from {StartDate} to {EndDate}", 
            accountId, startDate, endDate);

        // Get all transactions for the account within the date range and before end date
        // We need transactions before the range to establish the starting balance
        var transactions = await _context.Transactions
            .Where(t => t.AccountId == accountId && t.TransactionDate <= endDate)
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.Id)
            .Select(t => new { t.TransactionDate, t.Balance })
            .ToListAsync();

        if (!transactions.Any())
        {
            _logger.LogInformation("No transactions found for account {AccountId}", accountId);
            return 0;
        }

        int snapshotsProcessed = 0;
        decimal? lastKnownBalance = null;

        // Process each day in the range
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            // Find the last transaction on or before this date
            var transactionOnOrBeforeDate = transactions
                .Where(t => t.TransactionDate <= date)
                .LastOrDefault();

            if (transactionOnOrBeforeDate != null)
            {
                lastKnownBalance = transactionOnOrBeforeDate.Balance;
            }

            // Only create snapshot if we have a balance to record
            if (lastKnownBalance.HasValue)
            {
                await UpsertSnapshotAsync(accountId, date, lastKnownBalance.Value);
                snapshotsProcessed++;
            }
        }

        _logger.LogInformation("Generated {Count} balance snapshots for account {AccountId}", 
            snapshotsProcessed, accountId);

        return snapshotsProcessed;
    }

    public async Task<int> GenerateSnapshotsForAllTransactionsAsync(int accountId)
    {
        _logger.LogInformation("Generating balance snapshots for all transactions in account {AccountId}", accountId);

        // Get the date range from transactions
        var dateRange = await _context.Transactions
            .Where(t => t.AccountId == accountId)
            .GroupBy(t => 1)
            .Select(g => new
            {
                MinDate = g.Min(t => t.TransactionDate),
                MaxDate = g.Max(t => t.TransactionDate)
            })
            .FirstOrDefaultAsync();

        if (dateRange == null)
        {
            _logger.LogInformation("No transactions found for account {AccountId}", accountId);
            return 0;
        }

        // Generate snapshots for the entire range
        return await GenerateSnapshotsAsync(accountId, dateRange.MinDate, dateRange.MaxDate);
    }

    public async Task<int> RegenerateSnapshotsForAccountsAsync(IEnumerable<int> accountIds)
    {
        int totalProcessed = 0;

        foreach (var accountId in accountIds)
        {
            var count = await GenerateSnapshotsForAllTransactionsAsync(accountId);
            totalProcessed += count;
        }

        return totalProcessed;
    }

    /// <summary>
    /// Creates or updates a balance snapshot for a specific date.
    /// Uses an efficient upsert pattern to avoid conflicts with the unique index.
    /// </summary>
    private async Task UpsertSnapshotAsync(int accountId, DateOnly snapshotDate, decimal balance)
    {
        // Try to find existing snapshot
        var existingSnapshot = await _context.BalanceSnapshots
            .FirstOrDefaultAsync(bs => bs.AccountId == accountId && bs.SnapshotDate == snapshotDate);

        if (existingSnapshot != null)
        {
            // Update existing snapshot if balance changed
            if (existingSnapshot.Balance != balance)
            {
                existingSnapshot.Balance = balance;
                existingSnapshot.CreatedAt = DateTime.UtcNow; // Update timestamp to reflect recalculation
                _logger.LogDebug("Updated snapshot for account {AccountId} on {Date}: {Balance}", 
                    accountId, snapshotDate, balance);
            }
        }
        else
        {
            // Create new snapshot
            var snapshot = new BalanceSnapshot
            {
                AccountId = accountId,
                SnapshotDate = snapshotDate,
                Balance = balance,
                CreatedAt = DateTime.UtcNow
            };

            _context.BalanceSnapshots.Add(snapshot);
            _logger.LogDebug("Created snapshot for account {AccountId} on {Date}: {Balance}", 
                accountId, snapshotDate, balance);
        }

        await _context.SaveChangesAsync();
    }
}
