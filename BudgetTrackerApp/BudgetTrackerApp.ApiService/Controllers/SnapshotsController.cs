using BudgetTrackerApp.ApiService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetTrackerApp.ApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SnapshotsController(ISnapshotService snapshotService) : ControllerBase
    {

        /// <summary>
        /// Generate balance snapshots for a specific account
        /// </summary>
        /// <param name="accountId">The account ID to generate snapshots for</param>
        /// <returns>Number of snapshots generated</returns>
        [HttpPost("generate/{accountId}")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> GenerateSnapshotsForAccount(int accountId)
        {
            var request = await snapshotService.GenerateSnapshotsForAllTransactionsAsync(accountId, HttpContext.RequestAborted);
            return request.ToActionResult(HttpContext);
        }

        /// <summary>
        /// Generate balance snapshots for all accounts the user has access to
        /// </summary>
        /// <returns>Number of snapshots generated</returns>
        [HttpPost("generate-all")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> GenerateSnapshotsForAllAccounts()
        {
            var request = await snapshotService.RegenerateSnapshotsForConnectedAccountsAsync(HttpContext.RequestAborted);
            return request.ToActionResult(HttpContext);
        }
    }
}
