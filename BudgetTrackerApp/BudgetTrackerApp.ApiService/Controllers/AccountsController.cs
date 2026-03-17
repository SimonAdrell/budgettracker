using System.Security.Claims;
using BudgetTrackerApp.ApiService.DTOs;
using BudgetTrackerApp.ApiService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetTrackerApp.ApiService.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/accounts")]
    public class AccountsController(IAccountService accountService) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(typeof(List<AccountDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAccounts()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            var accounts = await accountService.GetUserAccountsAsync(userId, HttpContext.RequestAborted);
            return Ok(accounts);
        }

        [HttpPost]
        [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateAccount(CreateAccountRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { error = "Account name is required" });
            }

            var account = await accountService.CreateAccountAsync(request, userId, HttpContext.RequestAborted);
            return Ok(account);
        }
    }
}
