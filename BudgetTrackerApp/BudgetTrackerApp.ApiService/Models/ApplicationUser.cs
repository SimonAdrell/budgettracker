using Microsoft.AspNetCore.Identity;

namespace BudgetTrackerApp.ApiService.Models;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property for account sharing
    public ICollection<AccountUser> AccountUsers { get; set; } = new List<AccountUser>();
}
