using BudgetTrackerApp.ApiService.DTOs;
using BudgetTrackerApp.ApiService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetTrackerApp.ApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TransactionsController(ITransactionService transactionService) : ControllerBase
    {
        [HttpGet("{accountId}")]
        [ProducesResponseType(typeof(List<TransactionListItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetForAccount(int accountId)
        {
            var response = await transactionService.GetAccountTransactionsAsync(accountId, HttpContext.RequestAborted);
            return response.ToActionResult(HttpContext);
        }
    }
}
