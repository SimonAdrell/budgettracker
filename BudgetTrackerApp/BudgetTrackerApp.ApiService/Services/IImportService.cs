using BudgetTrackerApp.ApiService.DTOs;

namespace BudgetTrackerApp.ApiService.Services;

public interface IImportService
{
    Task<ImportResponse> ImportTransactionsFromExcelAsync(Stream fileStream, int accountId, string userId);
    Task<List<TransactionImportDto>> ParseExcelFileAsync(Stream fileStream);
    Task<ImportValidationResult> ValidateImportDataAsync(List<TransactionImportDto> transactions);
}
