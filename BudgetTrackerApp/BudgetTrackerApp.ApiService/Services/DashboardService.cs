using BudgetTrackerApp.ApiService.Data;
using BudgetTrackerApp.ApiService.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BudgetTrackerApp.ApiService.Services;

public interface IDashboardService
{
    Task<ServiceResponse<AccountDashboardDto>> GetAccountDashboardAsync(int accountId, CancellationToken cancellationToken);
}

public class DashboardService(
    ApplicationDbContext context,
    IServiceGuard serviceGuard) : IDashboardService
{
    public async Task<ServiceResponse<AccountDashboardDto>> GetAccountDashboardAsync(int accountId, CancellationToken cancellationToken)
    {
        if (!await serviceGuard.UserHasAccessToAccount(accountId))
        {
            return ServiceResponse<AccountDashboardDto>.Unauthorized("You do not have access to this account");
        }

        var account = await context.Accounts
            .AsNoTracking()
            .Where(a => a.Id == accountId)
            .Select(a => new
            {
                a.Id,
                a.Name
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (account is null)
        {
            return ServiceResponse<AccountDashboardDto>.NotFound("Account not found");
        }

        var recentTransactions = await context.Transactions
            .AsNoTracking()
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.BookingDate)
            .ThenByDescending(t => t.Id)
            .Select(t => new DashboardRecentTransactionDto(
                t.TransactionDate,
                t.Description,
                t.Amount,
                t.Balance))
            .Take(6)
            .ToListAsync(cancellationToken);

        var transactionCount = await context.Transactions
            .AsNoTracking()
            .CountAsync(t => t.AccountId == accountId, cancellationToken);

        var latestTransaction = recentTransactions.FirstOrDefault();
        var dashboard = new AccountDashboardDto(
            AccountId: account.Id,
            AccountName: account.Name,
            CurrentBalance: latestTransaction?.Balance ?? 0m,
            LastUpdated: latestTransaction?.Date,
            TransactionCount: transactionCount,
            RecentTransactions: recentTransactions,
            HasTransactions: transactionCount > 0);

        return ServiceResponse<AccountDashboardDto>.Success(dashboard);
    }
}
