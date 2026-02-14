using BudgetTrackerApp.ApiService.DTOs;

namespace BudgetTrackerApp.ApiService.Services.Imports;

public interface ITransactionImport
{
    public List<TransactionImportDto> ImportTransactions(Stream fileStream);
}
