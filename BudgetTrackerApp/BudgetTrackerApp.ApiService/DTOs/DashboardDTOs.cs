namespace BudgetTrackerApp.ApiService.DTOs;

public record AccountDashboardDto(
    int AccountId,
    string AccountName,
    decimal CurrentBalance,
    DateOnly? LastUpdated,
    int TransactionCount,
    List<DashboardRecentTransactionDto> RecentTransactions,
    bool HasTransactions);

public record DashboardRecentTransactionDto(
    DateOnly Date,
    string Description,
    decimal Amount,
    decimal? Balance);
