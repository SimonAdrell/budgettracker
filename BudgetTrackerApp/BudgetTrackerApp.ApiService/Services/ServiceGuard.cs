namespace BudgetTrackerApp.ApiService.Services;

public interface IServiceGuard
{
    Task<bool> UserHasAccessToAccount(int accountId);
    bool UserIsValid();
    string? GetValidUser();
}

public class ServiceGuard(IHttpContextAccessor httpContextAccessor, IAccountService accountService) : IServiceGuard
{
    public async Task<bool> UserHasAccessToAccount(int accountId)
    {
        if (httpContextAccessor.HttpContext is not HttpContext httpContext)
        {
            return false;
        }

        if (httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value is not string userId)
        {
            return false;
        }

        if (!await accountService.UserHasAccessToAccountAsync(accountId, userId, httpContext.RequestAborted))
        {
            return false;
        }

        return true;

    }

    public bool UserIsValid()
    {
        if (httpContextAccessor.HttpContext is not HttpContext httpContext)
        {
            return false;
        }

        if (httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value is null)
        {
            return false;
        }
        return true;
    }

    public string? GetValidUser()
    {
        if (httpContextAccessor.HttpContext is not HttpContext httpContext)
        {
            return null;
        }

        if (httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value is not string userId)
        {
            return null;
        }

        return userId;
    }
}
