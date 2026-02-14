using BudgetTrackerApp.ApiService.DTOs;
using BudgetTrackerApp.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace BudgetTrackerApp.ApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImportController(IImportService importService) : ControllerBase
    {
        [HttpPost("upload")]
        [ProducesResponseType(typeof(ImportResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Upload()
        {
            var response = await importService.ImportTransactionsFromExcelAsync(HttpContext.RequestAborted);
            return response.ToActionResult(HttpContext);
        }
    }
}
