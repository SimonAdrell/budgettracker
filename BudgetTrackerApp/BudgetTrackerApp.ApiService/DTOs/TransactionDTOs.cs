namespace BudgetTrackerApp.ApiService.DTOs;

public class TransactionListItemDto
{
    public int Id { get; set; }
    public DateOnly BookingDate { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Balance { get; set; }
}
