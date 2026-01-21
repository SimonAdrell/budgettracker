using BudgetTrackerApp.ApiService.Data;
using BudgetTrackerApp.ApiService.Models;
using BudgetTrackerApp.ApiService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace BudgetTrackerApp.Tests;

#pragma warning disable xUnit1051 // CancellationToken not required for unit tests

public class BalanceSnapshotServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly BalanceSnapshotService _service;
    private readonly Mock<ILogger<BalanceSnapshotService>> _loggerMock;

    public BalanceSnapshotServiceTests()
    {
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<BalanceSnapshotService>>();
        _service = new BalanceSnapshotService(_context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GenerateSnapshotsAsync_WithNoTransactions_ReturnsZero()
    {
        // Arrange
        int accountId = 1;
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 1, 31);

        // Act
        var result = await _service.GenerateSnapshotsAsync(accountId, startDate, endDate);

        // Assert
        Assert.Equal(0, result);
        var snapshots = await _context.BalanceSnapshots.ToListAsync();
        Assert.Empty(snapshots);
    }

    [Fact]
    public async Task GenerateSnapshotsAsync_WithSingleTransaction_CreatesSnapshots()
    {
        // Arrange
        int accountId = 1;
        var transactionDate = new DateOnly(2024, 1, 15);
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 1, 31);

        // Create account
        var account = new Account { Id = accountId, Name = "Test Account" };
        _context.Accounts.Add(account);

        // Add a transaction
        var transaction = new Transaction
        {
            AccountId = accountId,
            TransactionDate = transactionDate,
            BookingDate = transactionDate,
            Description = "Test Transaction",
            Amount = 100.00m,
            Balance = 1000.00m
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GenerateSnapshotsAsync(accountId, startDate, endDate);

        // Assert
        // Should create snapshots from the first transaction date (Jan 15) to end date (Jan 31)
        // That's 17 days: Jan 15-31 inclusive
        Assert.Equal(17, result);
        var snapshots = await _context.BalanceSnapshots
            .Where(s => s.AccountId == accountId)
            .OrderBy(s => s.SnapshotDate)
            .ToListAsync();

        Assert.Equal(17, snapshots.Count);
        // All snapshots should have balance of 1000.00 since there are no other transactions
        Assert.All(snapshots, s => Assert.Equal(1000.00m, s.Balance));
        Assert.Equal(transactionDate, snapshots.First().SnapshotDate);
        Assert.Equal(endDate, snapshots.Last().SnapshotDate);
    }

    [Fact]
    public async Task GenerateSnapshotsAsync_WithMultipleTransactions_CalculatesCorrectBalances()
    {
        // Arrange
        int accountId = 1;
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 1, 5);

        // Create account
        var account = new Account { Id = accountId, Name = "Test Account" };
        _context.Accounts.Add(account);

        // Add transactions on different days
        _context.Transactions.AddRange(
            new Transaction
            {
                AccountId = accountId,
                TransactionDate = new DateOnly(2024, 1, 1),
                BookingDate = new DateOnly(2024, 1, 1),
                Description = "Opening Balance",
                Amount = 1000.00m,
                Balance = 1000.00m
            },
            new Transaction
            {
                AccountId = accountId,
                TransactionDate = new DateOnly(2024, 1, 2),
                BookingDate = new DateOnly(2024, 1, 2),
                Description = "Deposit",
                Amount = 500.00m,
                Balance = 1500.00m
            },
            new Transaction
            {
                AccountId = accountId,
                TransactionDate = new DateOnly(2024, 1, 4),
                BookingDate = new DateOnly(2024, 1, 4),
                Description = "Withdrawal",
                Amount = -200.00m,
                Balance = 1300.00m
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GenerateSnapshotsAsync(accountId, startDate, endDate);

        // Assert
        Assert.Equal(5, result);
        var snapshots = await _context.BalanceSnapshots
            .Where(s => s.AccountId == accountId)
            .OrderBy(s => s.SnapshotDate)
            .ToListAsync();

        Assert.Equal(5, snapshots.Count);
        Assert.Equal(1000.00m, snapshots[0].Balance); // Jan 1
        Assert.Equal(1500.00m, snapshots[1].Balance); // Jan 2
        Assert.Equal(1500.00m, snapshots[2].Balance); // Jan 3 (no transaction, uses previous)
        Assert.Equal(1300.00m, snapshots[3].Balance); // Jan 4
        Assert.Equal(1300.00m, snapshots[4].Balance); // Jan 5 (no transaction, uses previous)
    }

    [Fact]
    public async Task GenerateSnapshotsAsync_WithMultipleTransactionsOnSameDay_UsesLastBalance()
    {
        // Arrange
        int accountId = 1;
        var date = new DateOnly(2024, 1, 1);

        // Create account
        var account = new Account { Id = accountId, Name = "Test Account" };
        _context.Accounts.Add(account);

        // Add multiple transactions on the same day
        _context.Transactions.AddRange(
            new Transaction
            {
                AccountId = accountId,
                TransactionDate = date,
                BookingDate = date,
                Description = "Transaction 1",
                Amount = 100.00m,
                Balance = 1100.00m
            },
            new Transaction
            {
                AccountId = accountId,
                TransactionDate = date,
                BookingDate = date,
                Description = "Transaction 2",
                Amount = 50.00m,
                Balance = 1150.00m
            },
            new Transaction
            {
                AccountId = accountId,
                TransactionDate = date,
                BookingDate = date,
                Description = "Transaction 3",
                Amount = -30.00m,
                Balance = 1120.00m
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GenerateSnapshotsAsync(accountId, date, date);

        // Assert
        Assert.Equal(1, result);
        var snapshot = await _context.BalanceSnapshots.FirstOrDefaultAsync(s => s.AccountId == accountId && s.SnapshotDate == date);
        Assert.NotNull(snapshot);
        Assert.Equal(1120.00m, snapshot.Balance); // Should use the last transaction's balance
    }

    [Fact]
    public async Task GenerateSnapshotsAsync_UpdatesExistingSnapshots()
    {
        // Arrange
        int accountId = 1;
        var date = new DateOnly(2024, 1, 1);

        // Create account
        var account = new Account { Id = accountId, Name = "Test Account" };
        _context.Accounts.Add(account);

        // Add existing snapshot
        var existingSnapshot = new BalanceSnapshot
        {
            AccountId = accountId,
            SnapshotDate = date,
            Balance = 500.00m
        };
        _context.BalanceSnapshots.Add(existingSnapshot);

        // Add transaction with different balance
        var transaction = new Transaction
        {
            AccountId = accountId,
            TransactionDate = date,
            BookingDate = date,
            Description = "New Transaction",
            Amount = 100.00m,
            Balance = 1000.00m
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GenerateSnapshotsAsync(accountId, date, date);

        // Assert
        Assert.Equal(1, result);
        var snapshot = await _context.BalanceSnapshots.FirstOrDefaultAsync(s => s.AccountId == accountId && s.SnapshotDate == date);
        Assert.NotNull(snapshot);
        Assert.Equal(1000.00m, snapshot.Balance); // Should be updated to new balance
    }

    [Fact]
    public async Task GenerateSnapshotsAsync_WithInvalidDateRange_ThrowsException()
    {
        // Arrange
        int accountId = 1;
        var startDate = new DateOnly(2024, 1, 31);
        var endDate = new DateOnly(2024, 1, 1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GenerateSnapshotsAsync(accountId, startDate, endDate));
    }

    [Fact]
    public async Task GenerateSnapshotsForAllTransactionsAsync_WithNoTransactions_ReturnsZero()
    {
        // Arrange
        int accountId = 1;

        // Act
        var result = await _service.GenerateSnapshotsForAllTransactionsAsync(accountId);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GenerateSnapshotsForAllTransactionsAsync_GeneratesForEntireTransactionRange()
    {
        // Arrange
        int accountId = 1;

        // Create account
        var account = new Account { Id = accountId, Name = "Test Account" };
        _context.Accounts.Add(account);

        // Add transactions spanning multiple days
        _context.Transactions.AddRange(
            new Transaction
            {
                AccountId = accountId,
                TransactionDate = new DateOnly(2024, 1, 1),
                BookingDate = new DateOnly(2024, 1, 1),
                Description = "First Transaction",
                Amount = 1000.00m,
                Balance = 1000.00m
            },
            new Transaction
            {
                AccountId = accountId,
                TransactionDate = new DateOnly(2024, 1, 15),
                BookingDate = new DateOnly(2024, 1, 15),
                Description = "Middle Transaction",
                Amount = 500.00m,
                Balance = 1500.00m
            },
            new Transaction
            {
                AccountId = accountId,
                TransactionDate = new DateOnly(2024, 1, 31),
                BookingDate = new DateOnly(2024, 1, 31),
                Description = "Last Transaction",
                Amount = -200.00m,
                Balance = 1300.00m
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GenerateSnapshotsForAllTransactionsAsync(accountId);

        // Assert
        Assert.Equal(31, result); // Should cover Jan 1 to Jan 31
        var snapshots = await _context.BalanceSnapshots
            .Where(s => s.AccountId == accountId)
            .OrderBy(s => s.SnapshotDate)
            .ToListAsync();

        Assert.Equal(31, snapshots.Count);
        Assert.Equal(new DateOnly(2024, 1, 1), snapshots.First().SnapshotDate);
        Assert.Equal(new DateOnly(2024, 1, 31), snapshots.Last().SnapshotDate);
    }

    [Fact]
    public async Task RegenerateSnapshotsForAccountsAsync_ProcessesMultipleAccounts()
    {
        // Arrange
        var accounts = new[]
        {
            new Account { Id = 1, Name = "Account 1" },
            new Account { Id = 2, Name = "Account 2" }
        };
        _context.Accounts.AddRange(accounts);

        // Add transactions for both accounts
        _context.Transactions.AddRange(
            new Transaction
            {
                AccountId = 1,
                TransactionDate = new DateOnly(2024, 1, 1),
                BookingDate = new DateOnly(2024, 1, 1),
                Description = "Account 1 Transaction",
                Amount = 1000.00m,
                Balance = 1000.00m
            },
            new Transaction
            {
                AccountId = 2,
                TransactionDate = new DateOnly(2024, 1, 1),
                BookingDate = new DateOnly(2024, 1, 1),
                Description = "Account 2 Transaction",
                Amount = 2000.00m,
                Balance = 2000.00m
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RegenerateSnapshotsForAccountsAsync(new[] { 1, 2 });

        // Assert
        Assert.Equal(2, result); // One snapshot per account
        var account1Snapshots = await _context.BalanceSnapshots.Where(s => s.AccountId == 1).ToListAsync();
        var account2Snapshots = await _context.BalanceSnapshots.Where(s => s.AccountId == 2).ToListAsync();

        Assert.Single(account1Snapshots);
        Assert.Single(account2Snapshots);
        Assert.Equal(1000.00m, account1Snapshots[0].Balance);
        Assert.Equal(2000.00m, account2Snapshots[0].Balance);
    }
}
