using BudgetTrackerApp.ApiService.Data;
using BudgetTrackerApp.ApiService.DTOs;
using BudgetTrackerApp.ApiService.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using BudgetTrackerApp.ApiService.Services.Imports;

namespace BudgetTrackerApp.ApiService.Services;

public interface IImportService
{
    Task<ServiceResponse<ImportResponse>> ImportTransactionsFromExcelAsync(CancellationToken cancellationToken);
}


public class ImportService : IImportService
{
    private readonly ApplicationDbContext _context;
    private readonly IAccountService _accountService;
    private readonly ISnapshotService _snapshotService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceGuard _serviceGuard;

    public ImportService(ApplicationDbContext context, IAccountService accountService, ISnapshotService snapshotService, IHttpContextAccessor httpContextAccessor, IServiceGuard serviceGuard)
    {
        _context = context;
        _accountService = accountService;

        // Register code pages for ExcelDataReader
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _snapshotService = snapshotService;
        _httpContextAccessor = httpContextAccessor;
        _serviceGuard = serviceGuard;
    }

    public async Task<ServiceResponse<ImportResponse>> ImportTransactionsFromExcelAsync(CancellationToken cancellationToken)
    {
        if (_httpContextAccessor.HttpContext is not HttpContext httpContext)
        {
            return ServiceResponse<ImportResponse>.Unauthorized("Invalid context");
        }

        if (_serviceGuard.GetValidUser() is not string userId)
        {
            return ServiceResponse<ImportResponse>.Unauthorized("Invalid user context");
        }


        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var file = form.Files["file"];
        var accountIdStr = form["accountId"].ToString();

        if (file == null || file.Length == 0)
        {
            return ServiceResponse<ImportResponse>.Invalid(Constants.ValidationErrors.ValidationError, new Dictionary<string, string[]> { { Constants.ValidationErrors.ImportErrorKey, ["No file uploaded"] } });
        }

        if (!int.TryParse(accountIdStr, out var accountId))
        {
            return ServiceResponse<ImportResponse>.Invalid(Constants.ValidationErrors.ValidationError, new Dictionary<string, string[]> { { Constants.ValidationErrors.AccountErrorKey, ["Invalid account ID"] } });
        }

        if (!await _accountService.UserHasAccessToAccountAsync(accountId, userId, cancellationToken))
        {
            return ServiceResponse<ImportResponse>.Forbid("You do not have access to this account");
        }

        // Check file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".xls" && extension != ".xlsx")
        {
            return ServiceResponse<ImportResponse>.Invalid(Constants.ValidationErrors.ValidationError, new Dictionary<string, string[]> { { Constants.ValidationErrors.ImportErrorKey, ["Only Excel files (.xls, .xlsx) are supported"] } });
        }

        using var stream = file.OpenReadStream();
        var response = new ImportResponse();

        try
        {
            // Parse Excel file
            var lfImporter = new LFImport();
            List<TransactionImportDto> transactions;
            try
            {
                transactions = lfImporter.ImportTransactions(stream);
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResponse<ImportResponse>.Invalid(
                    Constants.ValidationErrors.ValidationError,
                    new Dictionary<string, string[]>
                    {
                        { Constants.ValidationErrors.ImportErrorKey, [ex.Message] }
                    });
            }

            if (lfImporter.RowWarnings.Count > 0)
            {
                response.Warnings.AddRange(lfImporter.RowWarnings);
                response.ErrorCount += lfImporter.RowWarnings.Count;
            }

            // Validate data
            var validation = ValidateImportData(transactions);
            if (!validation.IsValid)
            {
                return ServiceResponse<ImportResponse>.Invalid(Constants.ValidationErrors.ValidationError, new Dictionary<string, string[]> { { Constants.ValidationErrors.ImportErrorKey, validation.Errors.ToArray() } });
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

            await _context.SaveChangesAsync(cancellationToken);

            // Generate balance snapshots after successful import persistence.
            // Note: We regenerate all snapshots to ensure correctness, as new transactions
            // may affect the balance history. For large accounts, consider implementing
            // a more targeted approach that only regenerates affected date ranges.
            if (response.ImportedCount > 0)
            {
                await _snapshotService.GenerateSnapshotsForAllTransactionsAsync(accountId, cancellationToken);
            }

            response.Success = true;
            return ServiceResponse<ImportResponse>.Success(response);
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Errors.Add($"Error importing transactions: {ex.Message}");
            response.ErrorCount++;
            return new ServiceResponse<ImportResponse>
            {
                Data = response,
                Message = "Import failed",
                ResponseType = ServiceResponseType.Failure
            };
        }
    }

    public ImportValidationResult ValidateImportData(List<TransactionImportDto> transactions)
    {
        var result = new ImportValidationResult { IsValid = true };

        if (transactions == null || transactions.Count == 0)
        {
            result.IsValid = false;
            result.Errors.Add("No transactions found in the file");
            return result;
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

        return result;
    }
}
