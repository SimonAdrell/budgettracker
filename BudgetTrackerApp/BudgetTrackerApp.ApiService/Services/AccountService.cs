using BudgetTrackerApp.ApiService.Data;
using BudgetTrackerApp.ApiService.DTOs;
using BudgetTrackerApp.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetTrackerApp.ApiService.Services;

public class AccountService : IAccountService
{
    private readonly ApplicationDbContext _context;

    public AccountService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<AccountDto>> GetUserAccountsAsync(string userId)
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
            .ToListAsync();

        return accounts;
    }

    public async Task<AccountDto?> GetAccountByIdAsync(int accountId, string userId)
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
            .FirstOrDefaultAsync();

        return account;
    }

    public async Task<AccountDto> CreateAccountAsync(CreateAccountRequest request, string userId)
    {
        var account = new Account
        {
            Name = request.Name,
            AccountNumber = request.AccountNumber,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        // Create AccountUser relationship with Owner role
        var accountUser = new AccountUser
        {
            UserId = userId,
            AccountId = account.Id,
            Role = "Owner",
            GrantedAt = DateTime.UtcNow
        };

        _context.AccountUsers.Add(accountUser);
        await _context.SaveChangesAsync();

        return new AccountDto
        {
            Id = account.Id,
            Name = account.Name,
            AccountNumber = account.AccountNumber,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
    }

    public async Task<bool> UserHasAccessToAccountAsync(int accountId, string userId)
    {
        return await _context.AccountUsers
            .AnyAsync(au => au.AccountId == accountId && au.UserId == userId);
    }
}
