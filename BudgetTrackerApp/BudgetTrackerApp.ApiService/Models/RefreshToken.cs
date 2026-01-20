using System.ComponentModel.DataAnnotations;

namespace BudgetTrackerApp.ApiService.Models;

public class RefreshToken
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public bool IsRevoked { get; set; }

    public bool IsUsed { get; set; }

    public virtual ApplicationUser? User { get; set; }
}
