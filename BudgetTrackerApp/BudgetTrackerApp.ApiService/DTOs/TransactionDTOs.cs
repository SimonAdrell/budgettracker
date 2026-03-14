namespace BudgetTrackerApp.ApiService.DTOs;

public record TransactionListItemDto(
    int Id,
    DateOnly BookingDate,
    DateOnly TransactionDate,
    string Description,
    decimal Amount,
    decimal Balance);
