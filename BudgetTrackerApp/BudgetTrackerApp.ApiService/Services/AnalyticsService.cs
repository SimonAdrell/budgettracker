using BudgetTrackerApp.ApiService.Data;
using BudgetTrackerApp.ApiService.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BudgetTrackerApp.ApiService.Services;

public interface IAnalyticsService
{
    Task<ServiceResponse<BalanceOverTimeResponse>> GetBalanceOverTimeAsync(string userId, AnalyticsQueryRequest request, CancellationToken cancellationToken);
    Task<ServiceResponse<IncomeVsExpensesResponse>> GetIncomeVsExpensesAsync(string userId, AnalyticsQueryRequest request, CancellationToken cancellationToken);
    Task<ServiceResponse<SpendingByCategoryResponse>> GetSpendingByCategoryAsync(string userId, AnalyticsQueryRequest request, CancellationToken cancellationToken);
    Task<ServiceResponse<CategorySpendingOverTimeResponse>> GetCategorySpendingOverTimeAsync(string userId, AnalyticsQueryRequest request, CancellationToken cancellationToken);
    Task<ServiceResponse<NetWorthOverTimeResponse>> GetNetWorthOverTimeAsync(string userId, AnalyticsQueryRequest request, CancellationToken cancellationToken);
}

public sealed record AnalyticsQueryContext(
    DateTime FromUtc,
    DateTime ToUtc,
    DateOnly FromDate,
    DateOnly ToDateInclusive,
    string Bucket,
    int? AccountId);

public class AnalyticsService(ApplicationDbContext context, IConfiguration configuration) : IAnalyticsService
{
    private const string UncategorizedName = "Uncategorized";
    private readonly string _currencyCode = configuration["Analytics:CurrencyCode"] ?? "USD";
    private const string InvalidQueryMessage = "Invalid analytics query";

    private static AnalyticsQueryContext NormalizeQuery(AnalyticsQueryRequest request)
    {
        var bucket = (request.Bucket ?? "month").Trim().ToLowerInvariant();
        if (bucket is not ("day" or "week" or "month"))
        {
            throw new ArgumentException("bucket must be one of: day, week, month");
        }

        if (request.FromUtc.HasValue && request.ToUtc.HasValue)
        {
            var rawFromUtc = request.FromUtc.Value.Kind == DateTimeKind.Utc
                ? request.FromUtc.Value
                : request.FromUtc.Value.ToUniversalTime();
            var rawToUtc = request.ToUtc.Value.Kind == DateTimeKind.Utc
                ? request.ToUtc.Value
                : request.ToUtc.Value.ToUniversalTime();

            if (rawFromUtc > rawToUtc)
            {
                throw new ArgumentException("fromUtc must be before or equal to toUtc");
            }
        }

        var nowUtc = DateTime.UtcNow;
        var startOfCurrentMonth = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        DateTime fromUtc;
        DateTime toUtcInclusive;

        if (!request.FromUtc.HasValue && !request.ToUtc.HasValue)
        {
            fromUtc = startOfCurrentMonth.AddMonths(-1);
            toUtcInclusive = startOfCurrentMonth.AddTicks(-1);
        }
        else
        {
            fromUtc = ToUtcStartOfDay(request.FromUtc ?? request.ToUtc ?? startOfCurrentMonth.AddMonths(-1));
            toUtcInclusive = ToUtcEndOfDay(request.ToUtc ?? request.FromUtc ?? startOfCurrentMonth.AddTicks(-1));
        }

        if (fromUtc > toUtcInclusive)
        {
            throw new ArgumentException("fromUtc must be before or equal to toUtc");
        }

        var fromAligned = AlignToBucketStart(fromUtc, bucket);
        var toAlignedInclusive = AlignToBucketStart(toUtcInclusive, bucket);
        var toExclusive = AddBucket(toAlignedInclusive, bucket);

        return new AnalyticsQueryContext(
            fromAligned,
            toExclusive,
            DateOnly.FromDateTime(fromAligned),
            DateOnly.FromDateTime(toExclusive.AddDays(-1)),
            bucket,
            request.AccountId);
    }

    public async Task<ServiceResponse<BalanceOverTimeResponse>> GetBalanceOverTimeAsync(string userId, AnalyticsQueryRequest request, CancellationToken cancellationToken)
    {
        if (!TryNormalizeQuery(request, out var query, out var validationMessage))
        {
            return InvalidQueryResponse<BalanceOverTimeResponse>(validationMessage);
        }

        var metadata = BuildMetadata(query);
        var bucketStarts = BuildBuckets(query.FromUtc, query.ToUtc, query.Bucket);
        var accountIds = await GetScopedAccountIdsAsync(userId, query.AccountId, cancellationToken);
        if (query.AccountId.HasValue && accountIds.Count == 0)
        {
            return ServiceResponse<BalanceOverTimeResponse>.Forbid("You do not have access to this account");
        }

        if (accountIds.Count == 0)
        {
            return ServiceResponse<BalanceOverTimeResponse>.Success(
                new BalanceOverTimeResponse(metadata, bucketStarts.Select(d => new BalanceOverTimePoint(d, 0m)).ToList()));
        }

        var maxDate = query.ToDateInclusive;
        var balanceProjections = await context.Transactions
            .AsNoTracking()
            .Where(t => accountIds.Contains(t.AccountId) && t.TransactionDate <= maxDate)
            .OrderBy(t => t.AccountId)
            .ThenBy(t => t.TransactionDate)
            .ThenBy(t => t.Id)
            .Select(t => new AccountBalanceProjection(t.AccountId, t.TransactionDate, t.Balance))
            .ToListAsync(cancellationToken);

        var points = BuildBalancePoints(bucketStarts, query.Bucket, accountIds, balanceProjections)
            .Select(x => new BalanceOverTimePoint(x.PeriodStartUtc, x.Balance))
            .ToList();

        return ServiceResponse<BalanceOverTimeResponse>.Success(new BalanceOverTimeResponse(metadata, points));
    }

    public async Task<ServiceResponse<IncomeVsExpensesResponse>> GetIncomeVsExpensesAsync(string userId, AnalyticsQueryRequest request, CancellationToken cancellationToken)
    {
        if (!TryNormalizeQuery(request, out var query, out var validationMessage))
        {
            return InvalidQueryResponse<IncomeVsExpensesResponse>(validationMessage);
        }

        var metadata = BuildMetadata(query);
        var bucketStarts = BuildBuckets(query.FromUtc, query.ToUtc, query.Bucket);
        var accountIds = await GetScopedAccountIdsAsync(userId, query.AccountId, cancellationToken);
        if (query.AccountId.HasValue && accountIds.Count == 0)
        {
            return ServiceResponse<IncomeVsExpensesResponse>.Forbid("You do not have access to this account");
        }

        var results = bucketStarts.ToDictionary(
            keySelector: s => s,
            elementSelector: _ => new IncomeAgg(0m, 0m));

        if (accountIds.Count > 0)
        {
            var transactions = await context.Transactions
                .AsNoTracking()
                .Where(t => accountIds.Contains(t.AccountId)
                    && t.TransactionDate >= query.FromDate
                    && t.TransactionDate <= query.ToDateInclusive)
                .ToListAsync(cancellationToken);

            foreach (var tx in transactions)
            {
                var bucketStart = AlignToBucketStart(tx.TransactionDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), query.Bucket);
                if (!results.ContainsKey(bucketStart))
                {
                    continue;
                }

                if (tx.Amount > 0)
                {
                    var current = results[bucketStart];
                    results[bucketStart] = current with { Income = current.Income + tx.Amount };
                }
                else if (tx.Amount < 0)
                {
                    var current = results[bucketStart];
                    results[bucketStart] = current with { Expenses = current.Expenses + Math.Abs(tx.Amount) };
                }
            }
        }

        var points = bucketStarts
            .Select(start =>
            {
                var agg = results[start];
                var net = agg.Income - agg.Expenses;
                return new IncomeVsExpensesPoint(start, agg.Income, agg.Expenses, net);
            })
            .ToList();

        return ServiceResponse<IncomeVsExpensesResponse>.Success(new IncomeVsExpensesResponse(metadata, points));
    }

    public async Task<ServiceResponse<SpendingByCategoryResponse>> GetSpendingByCategoryAsync(string userId, AnalyticsQueryRequest request, CancellationToken cancellationToken)
    {
        if (!TryNormalizeQuery(request, out var query, out var validationMessage))
        {
            return InvalidQueryResponse<SpendingByCategoryResponse>(validationMessage);
        }

        var metadata = BuildMetadata(query);
        var accountIds = await GetScopedAccountIdsAsync(userId, query.AccountId, cancellationToken);
        if (query.AccountId.HasValue && accountIds.Count == 0)
        {
            return ServiceResponse<SpendingByCategoryResponse>.Forbid("You do not have access to this account");
        }

        if (accountIds.Count == 0)
        {
            return ServiceResponse<SpendingByCategoryResponse>.Success(new SpendingByCategoryResponse(metadata, []));
        }

        var rows = await context.Transactions
            .AsNoTracking()
            .Where(t => accountIds.Contains(t.AccountId)
                && t.TransactionDate >= query.FromDate
                && t.TransactionDate <= query.ToDateInclusive
                && t.Amount < 0)
            .Select(t => new
            {
                t.CategoryId,
                CategoryName = t.Category != null ? t.Category.Name : UncategorizedName,
                t.Amount
            })
            .ToListAsync(cancellationToken);

        var groupedRows = rows
            .GroupBy(r => new { r.CategoryId, r.CategoryName })
            .Select(g => new SpendingByCategoryRow(g.Key.CategoryId, g.Key.CategoryName, g.Sum(x => Math.Abs(x.Amount))))
            .OrderByDescending(r => r.Amount)
            .ToList();

        return ServiceResponse<SpendingByCategoryResponse>.Success(new SpendingByCategoryResponse(metadata, groupedRows));
    }

    public async Task<ServiceResponse<CategorySpendingOverTimeResponse>> GetCategorySpendingOverTimeAsync(string userId, AnalyticsQueryRequest request, CancellationToken cancellationToken)
    {
        if (!TryNormalizeQuery(request, out var query, out var validationMessage))
        {
            return InvalidQueryResponse<CategorySpendingOverTimeResponse>(validationMessage);
        }

        var metadata = BuildMetadata(query);
        var bucketStarts = BuildBuckets(query.FromUtc, query.ToUtc, query.Bucket);
        var accountIds = await GetScopedAccountIdsAsync(userId, query.AccountId, cancellationToken);
        if (query.AccountId.HasValue && accountIds.Count == 0)
        {
            return ServiceResponse<CategorySpendingOverTimeResponse>.Forbid("You do not have access to this account");
        }

        var map = bucketStarts.ToDictionary(s => s, _ => new Dictionary<(int?, string), decimal>());
        if (accountIds.Count > 0)
        {
            var rows = await context.Transactions
                .AsNoTracking()
                .Where(t => accountIds.Contains(t.AccountId)
                    && t.TransactionDate >= query.FromDate
                    && t.TransactionDate <= query.ToDateInclusive
                    && t.Amount < 0)
                .Select(t => new
                {
                    t.TransactionDate,
                    t.CategoryId,
                    CategoryName = t.Category != null ? t.Category.Name : UncategorizedName,
                    t.Amount
                })
                .ToListAsync(cancellationToken);

            foreach (var row in rows)
            {
                var bucketStart = AlignToBucketStart(row.TransactionDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), query.Bucket);
                if (!map.TryGetValue(bucketStart, out var bucketCategoryMap))
                {
                    continue;
                }

                var key = (row.CategoryId, row.CategoryName);
                bucketCategoryMap.TryGetValue(key, out var currentAmount);
                bucketCategoryMap[key] = currentAmount + Math.Abs(row.Amount);
            }
        }

        var points = bucketStarts
            .Select(start =>
            {
                var categories = map[start]
                    .OrderByDescending(x => x.Value)
                    .ThenBy(x => x.Key.Item2)
                    .Select(x => new CategorySpendingBreakdown(x.Key.Item1, x.Key.Item2, x.Value))
                    .ToList();
                return new CategorySpendingOverTimePoint(start, categories);
            })
            .ToList();

        return ServiceResponse<CategorySpendingOverTimeResponse>.Success(new CategorySpendingOverTimeResponse(metadata, points));
    }

    public async Task<ServiceResponse<NetWorthOverTimeResponse>> GetNetWorthOverTimeAsync(string userId, AnalyticsQueryRequest request, CancellationToken cancellationToken)
    {
        var balance = await GetBalanceOverTimeAsync(userId, request, cancellationToken);
        if (balance.ResponseType != ServiceResponseType.Success || balance.Data is null)
        {
            return FromNonSuccess<BalanceOverTimeResponse, NetWorthOverTimeResponse>(balance);
        }

        var points = balance.Data.Points
            .Select(p => new NetWorthOverTimePoint(p.PeriodStartUtc, p.Balance))
            .ToList();
        return ServiceResponse<NetWorthOverTimeResponse>.Success(new NetWorthOverTimeResponse(balance.Data.Metadata, points));
    }

    private AnalyticsResponseMetadata BuildMetadata(AnalyticsQueryContext context)
    {
        return new AnalyticsResponseMetadata(context.FromUtc, context.ToUtc, context.Bucket, _currencyCode);
    }

    private async Task<List<int>> GetScopedAccountIdsAsync(string userId, int? accountId, CancellationToken cancellationToken)
    {
        var query = context.AccountUsers
            .AsNoTracking()
            .Where(au => au.UserId == userId);

        if (accountId.HasValue)
        {
            query = query.Where(au => au.AccountId == accountId.Value);
        }

        return await query
            .Select(au => au.AccountId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private static DateTime ToUtcStartOfDay(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        return new DateTime(utc.Year, utc.Month, utc.Day, 0, 0, 0, DateTimeKind.Utc);
    }

    private static DateTime ToUtcEndOfDay(DateTime value)
    {
        return ToUtcStartOfDay(value).AddDays(1).AddTicks(-1);
    }

    private static DateTime AlignToBucketStart(DateTime utcDateTime, string bucket)
    {
        var normalized = ToUtcStartOfDay(utcDateTime);
        return bucket switch
        {
            "day" => normalized,
            "week" => normalized.AddDays(-((int)normalized.DayOfWeek + 6) % 7),
            "month" => new DateTime(normalized.Year, normalized.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            _ => throw new ArgumentException("Unsupported bucket")
        };
    }

    private static DateTime AddBucket(DateTime utcDateTime, string bucket)
    {
        return bucket switch
        {
            "day" => utcDateTime.AddDays(1),
            "week" => utcDateTime.AddDays(7),
            "month" => utcDateTime.AddMonths(1),
            _ => throw new ArgumentException("Unsupported bucket")
        };
    }

    private static List<DateTime> BuildBuckets(DateTime fromUtc, DateTime toUtcExclusive, string bucket)
    {
        var points = new List<DateTime>();
        for (var cursor = fromUtc; cursor < toUtcExclusive; cursor = AddBucket(cursor, bucket))
        {
            points.Add(cursor);
        }

        return points;
    }

    private static List<BalancePointProjection> BuildBalancePoints(
        IReadOnlyList<DateTime> bucketStarts,
        string bucket,
        IReadOnlyList<int> accountIds,
        IReadOnlyList<AccountBalanceProjection> transactions)
    {
        if (bucketStarts.Count == 0)
        {
            return [];
        }

        var groupedByAccount = transactions
            .GroupBy(t => t.AccountId)
            .ToDictionary(g => g.Key, g => g.OrderBy(t => t.TransactionDate).ToList());

        var pointers = accountIds.ToDictionary(id => id, _ => 0);
        var lastBalances = accountIds.ToDictionary(id => id, _ => 0m);

        var points = new List<BalancePointProjection>(bucketStarts.Count);
        for (var i = 0; i < bucketStarts.Count; i++)
        {
            var bucketStart = bucketStarts[i];
            var bucketEndDate = DateOnly.FromDateTime(AddBucket(bucketStart, bucket).AddDays(-1));

            foreach (var accountId in accountIds)
            {
                if (!groupedByAccount.TryGetValue(accountId, out var accountTransactions))
                {
                    continue;
                }

                var pointer = pointers[accountId];
                while (pointer < accountTransactions.Count && accountTransactions[pointer].TransactionDate <= bucketEndDate)
                {
                    lastBalances[accountId] = accountTransactions[pointer].Balance;
                    pointer++;
                }

                pointers[accountId] = pointer;
            }

            points.Add(new BalancePointProjection(bucketStart, lastBalances.Values.Sum()));
        }

        return points;
    }

    private sealed record AccountBalanceProjection(int AccountId, DateOnly TransactionDate, decimal Balance);
    private sealed record BalancePointProjection(DateTime PeriodStartUtc, decimal Balance);
    private sealed record IncomeAgg(decimal Income, decimal Expenses);

    private static bool TryNormalizeQuery(AnalyticsQueryRequest request, out AnalyticsQueryContext context, out string validationMessage)
    {
        try
        {
            context = NormalizeQuery(request);
            validationMessage = string.Empty;
            return true;
        }
        catch (ArgumentException ex)
        {
            context = null!;
            validationMessage = ex.Message;
            return false;
        }
    }

    private static ServiceResponse<T> InvalidQueryResponse<T>(string message)
    {
        return ServiceResponse<T>.Invalid(InvalidQueryMessage, new Dictionary<string, string[]>
        {
            { "query", [message] }
        });
    }

    private static ServiceResponse<TTarget> FromNonSuccess<TSource, TTarget>(ServiceResponse<TSource> source)
    {
        return new ServiceResponse<TTarget>
        {
            Message = source.Message,
            Extensions = source.Extensions,
            ResponseType = source.ResponseType
        };
    }
}
