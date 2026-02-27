using BudgetTrackerApp.ApiService.Data;
using BudgetTrackerApp.ApiService.DTOs;
using BudgetTrackerApp.ApiService.Models;
using BudgetTrackerApp.ApiService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BudgetTrackerApp.Tests;

public class AnalyticsServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AnalyticsService _service;
    private readonly string _userId = Guid.NewGuid().ToString();
    private readonly int _accountId = 100;

    public AnalyticsServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Analytics:CurrencyCode"] = "USD"
            })
            .Build();

        _service = new AnalyticsService(_context, configuration);

        _context.Accounts.Add(new Account { Id = _accountId, Name = "Test Account" });
        _context.AccountUsers.Add(new AccountUser
        {
            AccountId = _accountId,
            UserId = _userId,
            Role = "Owner",
            GrantedAt = DateTime.UtcNow
        });
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task IncomeVsExpenses_ZeroFillsMissingBuckets()
    {
        var request = new AnalyticsQueryRequest
        {
            FromUtc = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            ToUtc = new DateTime(2026, 01, 03, 0, 0, 0, DateTimeKind.Utc),
            Bucket = "day",
            AccountId = _accountId
        };

        var response = await _service.GetIncomeVsExpensesAsync(_userId, request, TestContext.Current.CancellationToken);

        Assert.Equal(3, response.Points.Count);
        Assert.All(response.Points, p =>
        {
            Assert.Equal(0m, p.Income);
            Assert.Equal(0m, p.Expenses);
            Assert.Equal(0m, p.Net);
        });
    }

    [Fact]
    public async Task IncomeVsExpenses_UsesRequiredSignConventions()
    {
        _context.Transactions.AddRange(
            new Transaction
            {
                AccountId = _accountId,
                BookingDate = new DateOnly(2026, 1, 1),
                TransactionDate = new DateOnly(2026, 1, 1),
                Description = "Salary",
                Amount = 200m,
                Balance = 1200m
            },
            new Transaction
            {
                AccountId = _accountId,
                BookingDate = new DateOnly(2026, 1, 1),
                TransactionDate = new DateOnly(2026, 1, 1),
                Description = "Groceries",
                Amount = -50m,
                Balance = 1150m
            });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new AnalyticsQueryRequest
        {
            FromUtc = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            ToUtc = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            Bucket = "day",
            AccountId = _accountId
        };

        var incomeResponse = await _service.GetIncomeVsExpensesAsync(_userId, request, TestContext.Current.CancellationToken);
        var spendingResponse = await _service.GetSpendingByCategoryAsync(_userId, request, TestContext.Current.CancellationToken);
        var netWorthResponse = await _service.GetNetWorthOverTimeAsync(_userId, request, TestContext.Current.CancellationToken);

        var point = Assert.Single(incomeResponse.Points);
        Assert.Equal(200m, point.Income);
        Assert.Equal(50m, point.Expenses);
        Assert.Equal(150m, point.Net);

        Assert.Equal(50m, Assert.Single(spendingResponse.Rows).Amount);
        Assert.Equal(1150m, Assert.Single(netWorthResponse.Points).NetWorth);
    }

    [Fact]
    public async Task SpendingByCategory_GroupsNullCategoryAsUncategorized()
    {
        var category = new Category { Name = "Food" };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _context.Transactions.AddRange(
            new Transaction
            {
                AccountId = _accountId,
                CategoryId = null,
                BookingDate = new DateOnly(2026, 1, 1),
                TransactionDate = new DateOnly(2026, 1, 1),
                Description = "Unknown purchase",
                Amount = -30m,
                Balance = 970m
            },
            new Transaction
            {
                AccountId = _accountId,
                CategoryId = category.Id,
                BookingDate = new DateOnly(2026, 1, 1),
                TransactionDate = new DateOnly(2026, 1, 1),
                Description = "Dinner",
                Amount = -20m,
                Balance = 950m
            });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new AnalyticsQueryRequest
        {
            FromUtc = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            ToUtc = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            Bucket = "day",
            AccountId = _accountId
        };

        var response = await _service.GetSpendingByCategoryAsync(_userId, request, TestContext.Current.CancellationToken);

        Assert.Equal(2, response.Rows.Count);
        var uncategorized = Assert.Single(response.Rows, r => r.CategoryName == "Uncategorized");
        Assert.Null(uncategorized.CategoryId);
        Assert.Equal(30m, uncategorized.Amount);
    }

    [Fact]
    public void NormalizeQuery_WhenRawFromIsAfterRawTo_ThrowsArgumentException()
    {
        var request = new AnalyticsQueryRequest
        {
            FromUtc = new DateTime(2026, 1, 2, 23, 0, 0, DateTimeKind.Utc),
            ToUtc = new DateTime(2026, 1, 2, 1, 0, 0, DateTimeKind.Utc),
            Bucket = "day",
            AccountId = _accountId
        };

        var ex = Assert.Throws<ArgumentException>(() => _service.NormalizeQuery(request));
        Assert.Contains("fromUtc must be before or equal to toUtc", ex.Message);
    }
}
