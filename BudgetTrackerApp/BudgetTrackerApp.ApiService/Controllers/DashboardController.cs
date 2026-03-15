using BudgetTrackerApp.ApiService.DTOs;
using BudgetTrackerApp.ApiService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetTrackerApp.ApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController(IDashboardService dashboardService) : ControllerBase
    {
        [HttpGet("{accountId}")]
        [ProducesResponseType(typeof(AccountDashboardDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetForAccount(int accountId)
        {
            var response = await dashboardService.GetAccountDashboardAsync(accountId, HttpContext.RequestAborted);
            return response.ToActionResult(HttpContext);
        }
    }
}
