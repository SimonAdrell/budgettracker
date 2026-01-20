namespace BudgetTrackerApp.ApiService.Models;

public class Account
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AccountNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<AccountUser> AccountUsers { get; set; } = new List<AccountUser>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<BalanceSnapshot> BalanceSnapshots { get; set; } = new List<BalanceSnapshot>();
}
