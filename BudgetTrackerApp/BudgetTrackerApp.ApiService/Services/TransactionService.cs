using BudgetTrackerApp.ApiService.Data;
using BudgetTrackerApp.ApiService.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BudgetTrackerApp.ApiService.Services;

public interface ITransactionService
{
    Task<ServiceResponse<List<TransactionListItemDto>>> GetAccountTransactionsAsync(int accountId, CancellationToken cancellationToken);
}

public class TransactionService(
    ApplicationDbContext context,
    IServiceGuard serviceGuard) : ITransactionService
{
    public async Task<ServiceResponse<List<TransactionListItemDto>>> GetAccountTransactionsAsync(int accountId, CancellationToken cancellationToken)
    {
        if (!await serviceGuard.UserHasAccessToAccount(accountId))
        {
            return ServiceResponse<List<TransactionListItemDto>>.Unauthorized("You do not have access to this account");
        }

        var transactions = await context.Transactions
            .AsNoTracking()
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.BookingDate)
            .ThenByDescending(t => t.Id)
            .Select(t => new TransactionListItemDto(
                t.Id,
                t.BookingDate,
                t.TransactionDate,
                t.Description,
                t.Amount,
                t.Balance))
            .ToListAsync(cancellationToken);

        return ServiceResponse<List<TransactionListItemDto>>.Success(transactions);
    }
}
