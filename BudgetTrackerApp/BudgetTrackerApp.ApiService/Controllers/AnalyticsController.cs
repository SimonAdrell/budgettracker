using BudgetTrackerApp.ApiService.DTOs;
using BudgetTrackerApp.ApiService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BudgetTrackerApp.ApiService.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/analytics")]
public class AnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    [HttpGet("balance-over-time")]
    [ProducesResponseType(typeof(BalanceOverTimeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBalanceOverTime([FromQuery] AnalyticsQueryRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var response = await analyticsService.GetBalanceOverTimeAsync(userId, request, cancellationToken);
        return response.ToActionResult(HttpContext);
    }

    [HttpGet("income-vs-expenses")]
    [ProducesResponseType(typeof(IncomeVsExpensesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetIncomeVsExpenses([FromQuery] AnalyticsQueryRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var response = await analyticsService.GetIncomeVsExpensesAsync(userId, request, cancellationToken);
        return response.ToActionResult(HttpContext);
    }

    [HttpGet("spending-by-category")]
    [ProducesResponseType(typeof(SpendingByCategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSpendingByCategory([FromQuery] AnalyticsQueryRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var response = await analyticsService.GetSpendingByCategoryAsync(userId, request, cancellationToken);
        return response.ToActionResult(HttpContext);
    }

    [HttpGet("category-spending-over-time")]
    [ProducesResponseType(typeof(CategorySpendingOverTimeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCategorySpendingOverTime([FromQuery] AnalyticsQueryRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var response = await analyticsService.GetCategorySpendingOverTimeAsync(userId, request, cancellationToken);
        return response.ToActionResult(HttpContext);
    }

    [HttpGet("net-worth-over-time")]
    [ProducesResponseType(typeof(NetWorthOverTimeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetNetWorthOverTime([FromQuery] AnalyticsQueryRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var response = await analyticsService.GetNetWorthOverTimeAsync(userId, request, cancellationToken);
        return response.ToActionResult(HttpContext);
    }
}
