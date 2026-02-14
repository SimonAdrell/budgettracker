namespace BudgetTrackerApp.ApiService.Services.Imports;

public record ImportColumns
{
    public int BookingDateCol { get; set; } = -1;
    public string BookingDateCulture { get; set; } = "sv-SE";
    public int TransactionDateCol { get; set; } = -1;
    public string TransactionDateCulture { get; set; } = "sv-SE";
    public int DescriptionCol { get; set; } = -1;
    public int AmountCol { get; set; } = -1;
    public int BalanceCol { get; set; } = -1;

    public bool IsValid()
    {
        return BookingDateCol != -1 && TransactionDateCol != -1 && DescriptionCol != -1 && AmountCol != -1 && BalanceCol != -1;
    }
}
