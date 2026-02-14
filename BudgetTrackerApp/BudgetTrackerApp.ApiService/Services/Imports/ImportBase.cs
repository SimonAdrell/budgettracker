using System.Globalization;
using BudgetTrackerApp.ApiService.DTOs;
using ExcelDataReader;

namespace BudgetTrackerApp.ApiService.Services.Imports;

public class ImportBase()
{
    public virtual ImportColumns MatchColumns(IExcelDataReader reader)
    {
        return new ImportColumns();
    }

    public IEnumerable<TransactionImportDto> ParseExcelFile(Stream fileStream)
    {
        using var reader = ExcelReaderFactory.CreateReader(fileStream);
        if (!reader.Read())
        {
            throw new InvalidOperationException("Excel file contains no data");
        }
        if (!reader.Read())
        {
            throw new InvalidOperationException("Excel file contains no data");
        }

        var matchedColumns = MatchColumns(reader);
        if (!matchedColumns.IsValid())
        {
            throw new InvalidOperationException("Excel file is missing required columns. Expected: Bokföringsdatum, Transaktionsdatum, Text, Insättning/Uttag, Behållning");
        }

        // Parse data rows (starting from row 3)
        while (reader.Read())
        {
            var transaction = ParseTransaction(reader, matchedColumns);
            if (transaction != null)
            {
                yield return transaction;
            }
        }
    }

    private static TransactionImportDto? ParseTransaction(IExcelDataReader reader, ImportColumns matchedColumns)
    {
        try
        {
            var bookingDateValue = reader.GetValue(matchedColumns.BookingDateCol);
            var transactionDateValue = reader.GetValue(matchedColumns.TransactionDateCol);
            var description = reader.GetValue(matchedColumns.DescriptionCol)?.ToString()?.Trim() ?? "";
            var amountValue = reader.GetValue(matchedColumns.AmountCol);
            var balanceValue = reader.GetValue(matchedColumns.BalanceCol);

            // Skip empty rows
            if (bookingDateValue == null && string.IsNullOrWhiteSpace(description))
                return null;

            return new TransactionImportDto
            {
                BookingDate = ExtractDate(bookingDateValue, matchedColumns.BookingDateCulture),
                TransactionDate = ExtractDate(transactionDateValue, matchedColumns.TransactionDateCulture),
                Description = description,
                Amount = ExtractDecimal(amountValue),
                Balance = ExtractDecimal(balanceValue),
                OriginalText = $"{bookingDateValue}|{transactionDateValue}|{description}|{amountValue}|{balanceValue}"
            };
        }
        catch (System.Exception)
        {
            return null;
        }
    }

    private static decimal ExtractDecimal(object? value)
    {
        decimal amount;
        if (value is double doubleValue)
        {
            amount = (decimal)doubleValue;
        }
        else if (value is decimal decimalValue)
        {
            amount = decimalValue;
        }
        else
        {
            var decimalText = value?.ToString()?.Trim() ?? "0";
            // Handle Swedish number format with comma as decimal separator
            var cleanValue = decimalText.Replace(" ", "").Replace(",", ".");
            if (!decimal.TryParse(cleanValue, NumberStyles.Any, CultureInfo.InvariantCulture, out amount))
            {
                amount = 0;
            }
        }

        return amount;
    }

    private static DateOnly ExtractDate(object? dateValue, string dateCulture = "sv-SE")
    {
        DateOnly date;
        if (dateValue is DateTime dateTime)
        {
            date = DateOnly.FromDateTime(dateTime);
        }
        else
        {
            var dateText = dateValue?.ToString()?.Trim() ?? "";
            if (!DateOnly.TryParse(dateText, new CultureInfo(dateCulture), out date))
            {
                throw new InvalidOperationException($"Invalid date");
            }
        }

        return date;
    }
}
