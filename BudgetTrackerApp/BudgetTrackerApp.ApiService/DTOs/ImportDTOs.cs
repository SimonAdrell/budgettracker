namespace BudgetTrackerApp.ApiService.DTOs;

public class AccountDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AccountNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateAccountRequest
{
    public string Name { get; set; } = string.Empty;
    public string? AccountNumber { get; set; }
}

public class TransactionImportDto
{
    public DateOnly BookingDate { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Balance { get; set; }
    public string? OriginalText { get; set; }
}

public class ImportResponse
{
    public bool Success { get; set; }
    public int ImportedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class ImportValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}
