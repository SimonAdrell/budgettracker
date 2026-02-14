using BudgetTrackerApp.ApiService.Data;
using BudgetTrackerApp.ApiService.DTOs;
using BudgetTrackerApp.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetTrackerApp.ApiService.Services;

public interface IAccountService
{
    Task<List<AccountDto>> GetUserAccountsAsync(string userId, CancellationToken cancellationToken);
    Task<AccountDto?> GetAccountByIdAsync(int accountId, string userId, CancellationToken cancellationToken);
    Task<AccountDto> CreateAccountAsync(CreateAccountRequest request, string userId, CancellationToken cancellationToken);
    Task<bool> UserHasAccessToAccountAsync(int accountId, string userId, CancellationToken cancellationToken);
}


public class AccountService : IAccountService
{
    private readonly ApplicationDbContext _context;

    public AccountService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<AccountDto>> GetUserAccountsAsync(string userId, CancellationToken cancellationToken)
    {
        var accounts = await _context.AccountUsers
            .Where(au => au.UserId == userId)
            .Include(au => au.Account)
            .Select(au => new AccountDto
            {
                Id = au.Account.Id,
                Name = au.Account.Name,
                AccountNumber = au.Account.AccountNumber,
                CreatedAt = au.Account.CreatedAt,
                UpdatedAt = au.Account.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return accounts;
    }

    public async Task<AccountDto?> GetAccountByIdAsync(int accountId, string userId, CancellationToken cancellationToken)
    {
        var account = await _context.AccountUsers
            .Where(au => au.AccountId == accountId && au.UserId == userId)
            .Include(au => au.Account)
            .Select(au => new AccountDto
            {
                Id = au.Account.Id,
                Name = au.Account.Name,
                AccountNumber = au.Account.AccountNumber,
                CreatedAt = au.Account.CreatedAt,
                UpdatedAt = au.Account.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return account;
    }

    public async Task<AccountDto> CreateAccountAsync(CreateAccountRequest request, string userId, CancellationToken cancellationToken)
    {
        var account = new Account
        {
            Name = request.Name,
            AccountNumber = request.AccountNumber,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync(cancellationToken);

        // Create AccountUser relationship with Owner role
        var accountUser = new AccountUser
        {
            UserId = userId,
            AccountId = account.Id,
            Role = "Owner",
            GrantedAt = DateTime.UtcNow
        };

        _context.AccountUsers.Add(accountUser);
        await _context.SaveChangesAsync(cancellationToken);

        return new AccountDto
        {
            Id = account.Id,
            Name = account.Name,
            AccountNumber = account.AccountNumber,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
    }

    public async Task<bool> UserHasAccessToAccountAsync(int accountId, string userId, CancellationToken cancellationToken)
    {
        return await _context.AccountUsers
            .AnyAsync(au => au.AccountId == accountId && au.UserId == userId, cancellationToken);
    }
}
