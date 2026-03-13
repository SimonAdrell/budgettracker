namespace BudgetTrackerApp.ApiService.DTOs;

public sealed record AnalyticsQueryRequest
{
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public string? Bucket { get; init; }
    public int? AccountId { get; init; }
}

public sealed record AnalyticsResponseMetadata(DateTime FromUtc, DateTime ToUtc, string Bucket, string CurrencyCode);

public sealed record BalanceOverTimePoint(DateTime PeriodStartUtc, decimal Balance);
public sealed record BalanceOverTimeResponse(AnalyticsResponseMetadata Metadata, IReadOnlyList<BalanceOverTimePoint> Points);

public sealed record IncomeVsExpensesPoint(DateTime PeriodStartUtc, decimal Income, decimal Expenses, decimal Net);
public sealed record IncomeVsExpensesResponse(AnalyticsResponseMetadata Metadata, IReadOnlyList<IncomeVsExpensesPoint> Points);

public sealed record SpendingByCategoryRow(int? CategoryId, string CategoryName, decimal Amount);
public sealed record SpendingByCategoryResponse(AnalyticsResponseMetadata Metadata, IReadOnlyList<SpendingByCategoryRow> Rows);

public sealed record CategorySpendingBreakdown(int? CategoryId, string CategoryName, decimal Amount);
public sealed record CategorySpendingOverTimePoint(DateTime PeriodStartUtc, IReadOnlyList<CategorySpendingBreakdown> Categories);
public sealed record CategorySpendingOverTimeResponse(AnalyticsResponseMetadata Metadata, IReadOnlyList<CategorySpendingOverTimePoint> Points);

public sealed record NetWorthOverTimePoint(DateTime PeriodStartUtc, decimal NetWorth);
public sealed record NetWorthOverTimeResponse(AnalyticsResponseMetadata Metadata, IReadOnlyList<NetWorthOverTimePoint> Points);
