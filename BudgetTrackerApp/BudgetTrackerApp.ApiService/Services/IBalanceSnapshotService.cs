namespace BudgetTrackerApp.ApiService.Services;

/// <summary>
/// Service for generating and managing balance snapshots for accounts.
/// Balance snapshots enable efficient querying and graphing of account balances over time.
/// </summary>
public interface IBalanceSnapshotService
{
    /// <summary>
    /// Generates daily balance snapshots for a specific account within a date range.
    /// Uses transaction data to calculate the balance at the end of each day.
    /// </summary>
    /// <param name="accountId">The account to generate snapshots for</param>
    /// <param name="startDate">Start date of the range (inclusive)</param>
    /// <param name="endDate">End date of the range (inclusive)</param>
    /// <returns>The number of snapshots created or updated</returns>
    Task<int> GenerateSnapshotsAsync(int accountId, DateOnly startDate, DateOnly endDate);

    /// <summary>
    /// Generates snapshots for all dates where transactions exist for an account.
    /// Useful for initial population or after bulk import.
    /// </summary>
    /// <param name="accountId">The account to generate snapshots for</param>
    /// <returns>The number of snapshots created or updated</returns>
    Task<int> GenerateSnapshotsForAllTransactionsAsync(int accountId);

    /// <summary>
    /// Regenerates snapshots for multiple accounts.
    /// Useful for bulk operations or system maintenance.
    /// </summary>
    /// <param name="accountIds">List of account IDs to process</param>
    /// <returns>Total number of snapshots created or updated</returns>
    Task<int> RegenerateSnapshotsForAccountsAsync(IEnumerable<int> accountIds);
}
