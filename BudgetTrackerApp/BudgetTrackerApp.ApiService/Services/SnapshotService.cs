using BudgetTrackerApp.ApiService.Data;
using BudgetTrackerApp.ApiService.DTOs;
using BudgetTrackerApp.ApiService.Extensions;
using BudgetTrackerApp.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetTrackerApp.ApiService.Services;


/// <summary>
/// Service for generating and managing balance snapshots for accounts.
/// Balance snapshots enable efficient querying and graphing of account balances over time.
/// </summary>
public interface ISnapshotService
{
    /// <summary>
    /// Generates daily balance snapshots for a specific account within a date range.
    /// Uses transaction data to calculate the balance at the end of each day.
    /// </summary>
    /// <param name="accountId">The account to generate snapshots for</param>
    /// <param name="startDate">Start date of the range (inclusive)</param>
    /// <param name="endDate">End date of the range (inclusive)</param>
    /// <returns>The number of snapshots created or updated</returns>
    Task<ServiceResponse<int>> GenerateSnapshotsAsync(int accountId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken);

    /// <summary>
    /// Generates snapshots for all dates where transactions exist for an account.
    /// Useful for initial population or after bulk import.
    /// </summary>
    /// <param name="accountId">The account to generate snapshots for</param>
    /// <returns>The number of snapshots created or updated</returns>
    Task<ServiceResponse<int>> GenerateSnapshotsForAllTransactionsAsync(int accountId, CancellationToken cancellationToken);

    /// <summary>
    /// Regenerates snapshots for multiple accounts.
    /// Useful for bulk operations or system maintenance.
    /// </summary>
    /// <param name="accountIds">List of account IDs to process</param>
    /// <returns>Total number of snapshots created or updated</returns>
    Task<ServiceResponse<int>> RegenerateSnapshotsForConnectedAccountsAsync(CancellationToken cancellationToken);
}


public class SnapshotService(ApplicationDbContext context, ILogger<SnapshotService> logger, IServiceGuard serviceGuard, IAccountService accountService) : ISnapshotService
{

    public async Task<ServiceResponse<int>> GenerateSnapshotsAsync(int accountId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
        if (!await serviceGuard.UserHasAccessToAccount(accountId))
        {
            return ServiceResponse<int>.Unauthorized("You do not have access to this account");
        }

        if (startDate > endDate)
        {
            throw new ArgumentException("Start date must be before or equal to end date");
        }

        logger.Informaion("Generating balance snapshots for account {AccountId} from {StartDate} to {EndDate}",
            accountId, startDate, endDate);

        // Get all transactions for the account within the date range and before end date
        // We need transactions before the range to establish the starting balance
        var transactions = await context.Transactions
            .Where(t => t.AccountId == accountId && t.TransactionDate <= endDate)
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.Id)
            .Select(t => new { t.TransactionDate, t.Balance })
            .ToListAsync();

        if (transactions.Count == 0)
        {
            logger.Informaion("No transactions found for account {AccountId}", accountId);
            return ServiceResponse<int>.Success(0);
        }

        int snapshotsProcessed = 0;
        decimal? lastKnownBalance = null;
        var snapshotsToUpsert = new List<(DateOnly date, decimal balance)>();

        // Process each day in the range
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            // Find the last transaction on or before this date
            var transactionOnOrBeforeDate = transactions
                .LastOrDefault(t => t.TransactionDate <= date);

            if (transactionOnOrBeforeDate != null)
            {
                lastKnownBalance = transactionOnOrBeforeDate.Balance;
            }

            // Only create snapshot if we have a balance to record
            if (lastKnownBalance.HasValue)
            {
                snapshotsToUpsert.Add((date, lastKnownBalance.Value));
                snapshotsProcessed++;
            }
        }

        // Batch upsert all snapshots
        await BatchUpsertSnapshotsAsync(accountId, snapshotsToUpsert);

        logger.Informaion("Generated {Count} balance snapshots for account {AccountId}",
            snapshotsProcessed, accountId);

        return ServiceResponse<int>.Success(snapshotsProcessed);
    }

    public async Task<ServiceResponse<int>> GenerateSnapshotsForAllTransactionsAsync(int accountId, CancellationToken cancellationToken)
    {
        if (!await serviceGuard.UserHasAccessToAccount(accountId))
        {
            return ServiceResponse<int>.Unauthorized("You do not have access to this account");
        }

        logger.Informaion("Generating balance snapshots for all transactions in account {AccountId}", accountId);

        // Get the date range from transactions
        var dateRange = await context.Transactions
            .Where(t => t.AccountId == accountId)
            .GroupBy(t => 1)
            .Select(g => new
            {
                MinDate = g.Min(t => t.TransactionDate),
                MaxDate = g.Max(t => t.TransactionDate)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (dateRange == null)
        {
            logger.Informaion("No transactions found for account {AccountId}", accountId);
            return ServiceResponse<int>.Success(0);
        }

        // Generate snapshots for the entire range
        return await GenerateSnapshotsAsync(accountId, dateRange.MinDate, dateRange.MaxDate, cancellationToken);
    }

    public async Task<ServiceResponse<int>> RegenerateSnapshotsForConnectedAccountsAsync(CancellationToken cancellationToken)
    {

        if (serviceGuard.GetValidUser() is not string userId)
        {
            return ServiceResponse<int>.Unauthorized("Invalid user context");
        }

        var accounts = await accountService.GetUserAccountsAsync(userId, cancellationToken);
        var accountIds = accounts.Select(a => a.Id).ToList();
        int totalProcessed = 0;

        foreach (var accountId in accountIds)
        {
            var generatedTransactionsResponse = await GenerateSnapshotsForAllTransactionsAsync(accountId, cancellationToken);
            if (generatedTransactionsResponse.ResponseType == ServiceResponseType.Success)
            {
                totalProcessed += generatedTransactionsResponse.Data;
            }
        }

        return ServiceResponse<int>.Success(totalProcessed);
    }

    /// <summary>
    /// Creates or updates a balance snapshot for a specific date.
    /// Uses an efficient upsert pattern to avoid conflicts with the unique index.
    /// </summary>
    private async Task UpsertSnapshotAsync(int accountId, DateOnly snapshotDate, decimal balance)
    {
        // Try to find existing snapshot
        var existingSnapshot = await context.BalanceSnapshots
            .FirstOrDefaultAsync(bs => bs.AccountId == accountId && bs.SnapshotDate == snapshotDate);

        if (existingSnapshot != null)
        {
            // Update existing snapshot if balance changed
            if (existingSnapshot.Balance != balance)
            {
                existingSnapshot.Balance = balance;
                existingSnapshot.CreatedAt = DateTime.UtcNow; // Update timestamp to reflect recalculation
                logger.Debug("Updated snapshot for account {AccountId} on {Date}: {Balance}",
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

            context.BalanceSnapshots.Add(snapshot);
            logger.Debug("Created snapshot for account {AccountId} on {Date}: {Balance}",
                accountId, snapshotDate, balance);
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Batch upserts multiple balance snapshots efficiently.
    /// Fetches all existing snapshots in the date range, then creates or updates them as needed.
    /// </summary>
    private async Task BatchUpsertSnapshotsAsync(int accountId, List<(DateOnly date, decimal balance)> snapshots)
    {
        if (!snapshots.Any())
        {
            return;
        }

        var startDate = snapshots.Min(s => s.date);
        var endDate = snapshots.Max(s => s.date);

        // Fetch all existing snapshots in the date range
        var existingSnapshots = await context.BalanceSnapshots
            .Where(bs => bs.AccountId == accountId && bs.SnapshotDate >= startDate && bs.SnapshotDate <= endDate)
            .ToDictionaryAsync(bs => bs.SnapshotDate, bs => bs);

        int updatedCount = 0;
        int createdCount = 0;

        foreach (var (date, balance) in snapshots)
        {
            if (existingSnapshots.TryGetValue(date, out var existingSnapshot))
            {
                // Update existing snapshot if balance changed
                if (existingSnapshot.Balance != balance)
                {
                    existingSnapshot.Balance = balance;
                    existingSnapshot.CreatedAt = DateTime.UtcNow;
                    updatedCount++;
                }
            }
            else
            {
                // Create new snapshot
                var snapshot = new BalanceSnapshot
                {
                    AccountId = accountId,
                    SnapshotDate = date,
                    Balance = balance,
                    CreatedAt = DateTime.UtcNow
                };

                context.BalanceSnapshots.Add(snapshot);
                createdCount++;
            }
        }

        await context.SaveChangesAsync();

        logger.Debug("Batch upserted snapshots for account {AccountId}: {CreatedCount} created, {UpdatedCount} updated",
            accountId, createdCount, updatedCount);
    }
}
