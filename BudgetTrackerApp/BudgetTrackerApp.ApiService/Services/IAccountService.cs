using BudgetTrackerApp.ApiService.DTOs;

namespace BudgetTrackerApp.ApiService.Services;

public interface IAccountService
{
    Task<List<AccountDto>> GetUserAccountsAsync(string userId);
    Task<AccountDto?> GetAccountByIdAsync(int accountId, string userId);
    Task<AccountDto> CreateAccountAsync(CreateAccountRequest request, string userId);
    Task<bool> UserHasAccessToAccountAsync(int accountId, string userId);
}
