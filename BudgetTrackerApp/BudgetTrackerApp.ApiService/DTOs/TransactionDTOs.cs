namespace BudgetTrackerApp.ApiService.DTOs;

public record AccountSummaryDto(
    decimal CurrentBalance,
    DateOnly? LastUpdatedDate,
    int TransactionCount);

public record TransactionListItemDto(
    int Id,
    DateOnly BookingDate,
    DateOnly TransactionDate,
    string Description,
    decimal Amount,
    decimal Balance);
