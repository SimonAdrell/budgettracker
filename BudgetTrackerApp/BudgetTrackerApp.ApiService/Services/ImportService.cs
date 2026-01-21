using BudgetTrackerApp.ApiService.Data;
using BudgetTrackerApp.ApiService.DTOs;
using BudgetTrackerApp.ApiService.Models;
using Microsoft.EntityFrameworkCore;
using ExcelDataReader;
using System.Text;

namespace BudgetTrackerApp.ApiService.Services;

public class ImportService : IImportService
{
    private readonly ApplicationDbContext _context;
    private readonly IAccountService _accountService;

    public ImportService(ApplicationDbContext context, IAccountService accountService)
    {
        _context = context;
        _accountService = accountService;
        
        // Register code pages for ExcelDataReader
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<ImportResponse> ImportTransactionsFromExcelAsync(Stream fileStream, int accountId, string userId)
    {
        var response = new ImportResponse();

        // Verify user has access to account
        if (!await _accountService.UserHasAccessToAccountAsync(accountId, userId))
        {
            response.Success = false;
            response.Errors.Add("You do not have access to this account");
            return response;
        }

        try
        {
            // Parse Excel file
            var transactions = await ParseExcelFileAsync(fileStream);

            // Validate data
            var validation = await ValidateImportDataAsync(transactions);
            if (!validation.IsValid)
            {
                response.Success = false;
                response.Errors.AddRange(validation.Errors);
                return response;
            }

            // Check for duplicates and import
            foreach (var transaction in transactions)
            {
                // Check for duplicate (same date, amount, and description)
                var isDuplicate = await _context.Transactions
                    .AnyAsync(t => t.AccountId == accountId &&
                                  t.BookingDate == transaction.BookingDate &&
                                  t.Amount == transaction.Amount &&
                                  t.Description == transaction.Description);

                if (isDuplicate)
                {
                    response.DuplicateCount++;
                    response.Warnings.Add($"Duplicate transaction skipped: {transaction.Description} on {transaction.BookingDate}");
                    continue;
                }

                // Create new transaction
                var newTransaction = new Transaction
                {
                    AccountId = accountId,
                    BookingDate = transaction.BookingDate,
                    TransactionDate = transaction.TransactionDate,
                    Description = transaction.Description,
                    Amount = transaction.Amount,
                    Balance = transaction.Balance,
                    OriginalText = transaction.OriginalText,
                    ImportedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Transactions.Add(newTransaction);
                response.ImportedCount++;
            }

            await _context.SaveChangesAsync();

            response.Success = true;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Errors.Add($"Error importing transactions: {ex.Message}");
            response.ErrorCount++;
        }

        return response;
    }

    public async Task<List<TransactionImportDto>> ParseExcelFileAsync(Stream fileStream)
    {
        var transactions = new List<TransactionImportDto>();

        using (var reader = ExcelReaderFactory.CreateReader(fileStream))
        {
            if (reader == null)
            {
                throw new InvalidOperationException("Unable to read Excel file");
            }

            // Read first worksheet
            if (!reader.Read())
            {
                throw new InvalidOperationException("Excel file contains no data");
            }

            // Find header row (typically first row)
            var bookingDateCol = -1;
            var transactionDateCol = -1;
            var descriptionCol = -1;
            var amountCol = -1;
            var balanceCol = -1;

            // Detect column headers (Swedish bank export format)
            for (int col = 0; col < reader.FieldCount; col++)
            {
                var header = reader.GetValue(col)?.ToString()?.Trim().ToLowerInvariant() ?? "";
                
                if (header.Contains("bokföringsdatum") || header.Contains("booking date"))
                    bookingDateCol = col;
                else if (header.Contains("transaktionsdatum") || header.Contains("transaction date"))
                    transactionDateCol = col;
                else if (header.Contains("text") || header.Contains("description"))
                    descriptionCol = col;
                else if (header.Contains("insättning") || header.Contains("uttag") || header.Contains("amount"))
                    amountCol = col;
                else if (header.Contains("behållning") || header.Contains("balance"))
                    balanceCol = col;
            }

            // Validate required columns
            if (bookingDateCol == -1 || transactionDateCol == -1 || descriptionCol == -1 || amountCol == -1 || balanceCol == -1)
            {
                throw new InvalidOperationException("Excel file is missing required columns. Expected: Bokföringsdatum, Transaktionsdatum, Text, Insättning/Uttag, Behållning");
            }

            // Parse data rows
            int rowNumber = 1;
            while (reader.Read())
            {
                rowNumber++;
                try
                {
                    var bookingDateValue = reader.GetValue(bookingDateCol);
                    var transactionDateValue = reader.GetValue(transactionDateCol);
                    var description = reader.GetValue(descriptionCol)?.ToString()?.Trim() ?? "";
                    var amountValue = reader.GetValue(amountCol);
                    var balanceValue = reader.GetValue(balanceCol);

                    // Skip empty rows
                    if (bookingDateValue == null && string.IsNullOrWhiteSpace(description))
                        continue;

                    // Parse booking date
                    DateOnly bookingDate;
                    if (bookingDateValue is DateTime bookingDateTime)
                    {
                        bookingDate = DateOnly.FromDateTime(bookingDateTime);
                    }
                    else
                    {
                        var bookingDateText = bookingDateValue?.ToString()?.Trim() ?? "";
                        if (!DateOnly.TryParse(bookingDateText, out bookingDate))
                        {
                            throw new InvalidOperationException($"Invalid booking date at row {rowNumber}");
                        }
                    }

                    // Parse transaction date
                    DateOnly transactionDate;
                    if (transactionDateValue is DateTime transactionDateTime)
                    {
                        transactionDate = DateOnly.FromDateTime(transactionDateTime);
                    }
                    else
                    {
                        var transactionDateText = transactionDateValue?.ToString()?.Trim() ?? "";
                        if (!DateOnly.TryParse(transactionDateText, out transactionDate))
                        {
                            transactionDate = bookingDate; // Default to booking date
                        }
                    }

                    // Parse amount
                    decimal amount = 0;
                    if (amountValue is double amountDouble)
                    {
                        amount = (decimal)amountDouble;
                    }
                    else if (amountValue is decimal amountDecimal)
                    {
                        amount = amountDecimal;
                    }
                    else
                    {
                        var amountText = amountValue?.ToString()?.Trim() ?? "0";
                        // Handle Swedish number format with comma as decimal separator
                        var amountCleaned = amountText.Replace(" ", "").Replace(",", ".");
                        if (!decimal.TryParse(amountCleaned, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out amount))
                        {
                            amount = 0;
                        }
                    }

                    // Parse balance
                    decimal balance = 0;
                    if (balanceValue is double balanceDouble)
                    {
                        balance = (decimal)balanceDouble;
                    }
                    else if (balanceValue is decimal balanceDecimal)
                    {
                        balance = balanceDecimal;
                    }
                    else
                    {
                        var balanceText = balanceValue?.ToString()?.Trim() ?? "0";
                        var balanceCleaned = balanceText.Replace(" ", "").Replace(",", ".");
                        if (!decimal.TryParse(balanceCleaned, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out balance))
                        {
                            balance = 0;
                        }
                    }

                    var transaction = new TransactionImportDto
                    {
                        BookingDate = bookingDate,
                        TransactionDate = transactionDate,
                        Description = description,
                        Amount = amount,
                        Balance = balance,
                        OriginalText = $"{bookingDateValue}|{transactionDateValue}|{description}|{amountValue}|{balanceValue}"
                    };

                    transactions.Add(transaction);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error parsing row {rowNumber}: {ex.Message}");
                }
            }
        }

        return await Task.FromResult(transactions);
    }

    public async Task<ImportValidationResult> ValidateImportDataAsync(List<TransactionImportDto> transactions)
    {
        var result = new ImportValidationResult { IsValid = true };

        if (transactions == null || transactions.Count == 0)
        {
            result.IsValid = false;
            result.Errors.Add("No transactions found in the file");
            return await Task.FromResult(result);
        }

        // Validate each transaction
        for (int i = 0; i < transactions.Count; i++)
        {
            var transaction = transactions[i];

            if (string.IsNullOrWhiteSpace(transaction.Description))
            {
                result.IsValid = false;
                result.Errors.Add($"Transaction {i + 1}: Description is required");
            }

            if (transaction.BookingDate == default)
            {
                result.IsValid = false;
                result.Errors.Add($"Transaction {i + 1}: Invalid booking date");
            }

            if (transaction.TransactionDate == default)
            {
                result.IsValid = false;
                result.Errors.Add($"Transaction {i + 1}: Invalid transaction date");
            }
        }

        return await Task.FromResult(result);
    }
}
