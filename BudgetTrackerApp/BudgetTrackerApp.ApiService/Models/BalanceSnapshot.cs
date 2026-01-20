namespace BudgetTrackerApp.ApiService.Models;

public class BalanceSnapshot
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public DateOnly SnapshotDate { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Account Account { get; set; } = null!;
}
