using BudgetTrackerApp.ApiService.Data;
using BudgetTrackerApp.ApiService.DTOs;
using BudgetTrackerApp.ApiService.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace BudgetTrackerApp.ApiService.Services;

public class ImportService : IImportService
{
    private readonly ApplicationDbContext _context;
    private readonly IAccountService _accountService;

    public ImportService(ApplicationDbContext context, IAccountService accountService)
    {
        _context = context;
        _accountService = accountService;
        
        // Set EPPlus license context
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
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

        using (var package = new ExcelPackage(fileStream))
        {
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            if (worksheet == null || worksheet.Dimension == null)
            {
                throw new InvalidOperationException("Excel file contains no worksheets or data");
            }

            var rowCount = worksheet.Dimension.Rows;
            
            // Find header row (typically row 1)
            var headerRow = 1;
            var bookingDateCol = -1;
            var transactionDateCol = -1;
            var descriptionCol = -1;
            var amountCol = -1;
            var balanceCol = -1;

            // Detect column headers (Swedish bank export format)
            for (int col = 1; col <= (worksheet.Dimension?.Columns ?? 0); col++)
            {
                var header = worksheet.Cells[headerRow, col].Text.Trim().ToLowerInvariant();
                
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
            for (int row = headerRow + 1; row <= rowCount; row++)
            {
                try
                {
                    var bookingDateText = worksheet.Cells[row, bookingDateCol].Text.Trim();
                    var transactionDateText = worksheet.Cells[row, transactionDateCol].Text.Trim();
                    var description = worksheet.Cells[row, descriptionCol].Text.Trim();
                    var amountText = worksheet.Cells[row, amountCol].Text.Trim();
                    var balanceText = worksheet.Cells[row, balanceCol].Text.Trim();

                    // Skip empty rows
                    if (string.IsNullOrWhiteSpace(bookingDateText) && string.IsNullOrWhiteSpace(description))
                        continue;

                    // Parse dates
                    if (!DateOnly.TryParse(bookingDateText, out var bookingDate))
                    {
                        // Try to get date value directly from Excel
                        var dateValue = worksheet.Cells[row, bookingDateCol].GetValue<DateTime?>();
                        if (dateValue.HasValue)
                            bookingDate = DateOnly.FromDateTime(dateValue.Value);
                        else
                            throw new InvalidOperationException($"Invalid booking date at row {row}");
                    }

                    if (!DateOnly.TryParse(transactionDateText, out var transactionDate))
                    {
                        var dateValue = worksheet.Cells[row, transactionDateCol].GetValue<DateTime?>();
                        if (dateValue.HasValue)
                            transactionDate = DateOnly.FromDateTime(dateValue.Value);
                        else
                            transactionDate = bookingDate; // Default to booking date
                    }

                    // Parse amount (handle Swedish number format with comma as decimal separator)
                    var amountCleaned = amountText.Replace(" ", "").Replace(",", ".");
                    if (!decimal.TryParse(amountCleaned, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var amount))
                    {
                        // Try to get numeric value directly
                        var numValue = worksheet.Cells[row, amountCol].GetValue<decimal?>();
                        amount = numValue ?? 0;
                    }

                    // Parse balance
                    var balanceCleaned = balanceText.Replace(" ", "").Replace(",", ".");
                    if (!decimal.TryParse(balanceCleaned, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var balance))
                    {
                        var numValue = worksheet.Cells[row, balanceCol].GetValue<decimal?>();
                        balance = numValue ?? 0;
                    }

                    var transaction = new TransactionImportDto
                    {
                        BookingDate = bookingDate,
                        TransactionDate = transactionDate,
                        Description = description,
                        Amount = amount,
                        Balance = balance,
                        OriginalText = $"{bookingDateText}|{transactionDateText}|{description}|{amountText}|{balanceText}"
                    };

                    transactions.Add(transaction);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error parsing row {row}: {ex.Message}");
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
