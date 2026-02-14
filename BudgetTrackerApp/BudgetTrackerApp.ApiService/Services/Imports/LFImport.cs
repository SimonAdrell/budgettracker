using BudgetTrackerApp.ApiService.DTOs;
using ExcelDataReader;

namespace BudgetTrackerApp.ApiService.Services.Imports;

public class LFImport : ImportBase, ITransactionImport
{
    public List<TransactionImportDto> ImportTransactions(Stream fileStream)
    {
        return [.. ParseExcelFile(fileStream)];
    }

    public override ImportColumns MatchColumns(IExcelDataReader reader)
    {
        var importColumns = new ImportColumns();
        for (int col = 0; col < reader.FieldCount; col++)
        {
            var header = reader.GetValue(col)?.ToString()?.Trim().ToLowerInvariant() ?? string.Empty;

            switch (header)
            {
                case var h when h.Contains("bokföringsdatum") || h.Contains("booking date"):
                    importColumns.BookingDateCol = col;
                    break;
                case var h when h.Contains("transaktionsdatum") || h.Contains("transaction date"):
                    importColumns.TransactionDateCol = col;
                    break;
                case var h when h.Contains("text") || h.Contains("description"):
                    importColumns.DescriptionCol = col;
                    break;
                case var h when h.Contains("insättning") || h.Contains("uttag") || h.Contains("amount"):
                    importColumns.AmountCol = col;
                    break;
                case var h when h.Contains("behållning") || h.Contains("balance"):
                    importColumns.BalanceCol = col;
                    break;
            }
        }
        return importColumns;
    }
}
