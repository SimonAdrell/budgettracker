namespace BudgetTrackerApp.ApiService.Models;

public class AccountUser
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int AccountId { get; set; }
    public string Role { get; set; } = string.Empty; // Owner, ReadOnly, ReadWrite
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public Account Account { get; set; } = null!;
}
