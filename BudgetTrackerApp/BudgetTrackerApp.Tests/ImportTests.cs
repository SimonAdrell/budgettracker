using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Net;
using System.IO.Compression;
using System.Security;
using System.Text;
using System.Text.Json;
using BudgetTrackerApp.ApiService.DTOs;
using Microsoft.Extensions.Logging;

namespace BudgetTrackerApp.Tests;

public class ImportTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task CanCreateAccountAndRetrieveAccounts()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var session = await CreateStartedAppSessionAsync(cancellationToken);
        var httpClient = session.HttpClient;

        await AuthenticateAsync(httpClient, cancellationToken);

        // Act - Create account
        var createdAccount = await CreateAccountAsync(httpClient, cancellationToken, "Test Account", "1234-5678");

        // Assert - Account creation
        Assert.NotNull(createdAccount);
        Assert.Equal("Test Account", createdAccount.Name);
        Assert.Equal("1234-5678", createdAccount.AccountNumber);

        // Act - Get accounts
        var getAccountsResponse = await httpClient.GetAsync("/api/accounts", cancellationToken);

        // Assert - Get accounts
        Assert.True(getAccountsResponse.IsSuccessStatusCode);
        
        var accounts = await getAccountsResponse.Content.ReadFromJsonAsync<List<AccountDto>>(cancellationToken);
        Assert.NotNull(accounts);
        Assert.Single(accounts);
        Assert.Equal("Test Account", accounts[0].Name);
    }

    [Fact]
    public async Task CannotAccessAccountsWithoutAuthentication()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var session = await CreateStartedAppSessionAsync(cancellationToken);
        var httpClient = session.HttpClient;

        // Act
        var getAccountsResponse = await httpClient.GetAsync("/api/accounts", cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, getAccountsResponse.StatusCode);
    }

    [Fact]
    public async Task CannotImportWithoutFile()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var session = await CreateStartedAppSessionAsync(cancellationToken);
        var httpClient = session.HttpClient;

        await AuthenticateAsync(httpClient, cancellationToken);

        // Act - Try to import without file
        using var formData = new MultipartFormDataContent();
        formData.Add(new StringContent("1"), "accountId");

        var importResponse = await httpClient.PostAsync("/api/import/upload", formData, cancellationToken);

        // Assert
        await AssertValidationProblemDetailsAsync(
            importResponse,
            expectedErrorKey: "ImportError",
            expectedErrorMessage: "No file uploaded",
            cancellationToken);
    }

    [Fact]
    public async Task ImportSkipsDuplicateTransactionsBasedOnBookingDateAmountAndDescription()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var session = await CreateStartedAppSessionAsync(cancellationToken);
        var httpClient = session.HttpClient;

        await AuthenticateAsync(httpClient, cancellationToken);
        var account = await CreateAccountAsync(httpClient, cancellationToken, "Duplicate Test Account", "5555-9999");

        var initialWorkbook = CreateLfWorkbookBytes(
            new ImportRow("2024-01-15", "2024-01-14", "Coffee Shop", "-45.67", "1000.00"));

        using var initialImportContent = CreateImportFormData(initialWorkbook, "initial-import.xlsx", account.Id.ToString());
        var initialImportResponse = await httpClient.PostAsync("/api/import/upload", initialImportContent, cancellationToken);
        Assert.True(
            initialImportResponse.IsSuccessStatusCode,
            $"Initial import failed: {await initialImportResponse.Content.ReadAsStringAsync(cancellationToken)}");

        var initialImportResult = await initialImportResponse.Content.ReadFromJsonAsync<ImportResponse>(cancellationToken);
        Assert.NotNull(initialImportResult);
        Assert.True(initialImportResult.Success);
        Assert.Equal(1, initialImportResult.ImportedCount);
        Assert.Equal(0, initialImportResult.DuplicateCount);
        Assert.Empty(initialImportResult.Warnings);

        var duplicateWorkbook = CreateLfWorkbookBytes(
            new ImportRow("2024-01-15", "2024-01-10", "Coffee Shop", "-45.67", "954.33"));

        using var duplicateImportContent = CreateImportFormData(duplicateWorkbook, "duplicate-import.xlsx", account.Id.ToString());
        var duplicateImportResponse = await httpClient.PostAsync("/api/import/upload", duplicateImportContent, cancellationToken);
        Assert.True(
            duplicateImportResponse.IsSuccessStatusCode,
            $"Duplicate import failed: {await duplicateImportResponse.Content.ReadAsStringAsync(cancellationToken)}");

        var duplicateImportResult = await duplicateImportResponse.Content.ReadFromJsonAsync<ImportResponse>(cancellationToken);
        Assert.NotNull(duplicateImportResult);
        Assert.True(duplicateImportResult.Success);
        Assert.Equal(0, duplicateImportResult.ImportedCount);
        Assert.Equal(1, duplicateImportResult.DuplicateCount);
        Assert.Single(duplicateImportResult.Warnings);
        Assert.Contains("Duplicate transaction skipped: Coffee Shop", duplicateImportResult.Warnings[0]);

        var transactionsResponse = await httpClient.GetAsync($"/api/transactions/{account.Id}", cancellationToken);
        Assert.True(
            transactionsResponse.IsSuccessStatusCode,
            $"Transaction read failed: {await transactionsResponse.Content.ReadAsStringAsync(cancellationToken)}");

        var transactions = await transactionsResponse.Content.ReadFromJsonAsync<List<TransactionListItemDto>>(cancellationToken);
        Assert.NotNull(transactions);
        Assert.Single(transactions);
        Assert.Equal(DateOnly.Parse("2024-01-15"), transactions[0].BookingDate);
        Assert.Equal(DateOnly.Parse("2024-01-14"), transactions[0].TransactionDate);
        Assert.Equal(-45.67m, transactions[0].Amount);
        Assert.Equal(1000.00m, transactions[0].Balance);
    }

    [Fact]
    public async Task CannotImportWithInvalidAccountId()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var session = await CreateStartedAppSessionAsync(cancellationToken);
        var httpClient = session.HttpClient;

        await AuthenticateAsync(httpClient, cancellationToken);

        var workbook = CreateLfWorkbookBytes(
            new ImportRow("2024-02-01", "2024-02-01", "Salary", "1000.00", "5000.00"));

        using var formData = CreateImportFormData(workbook, "invalid-account.xlsx", "not-an-account-id");
        var importResponse = await httpClient.PostAsync("/api/import/upload", formData, cancellationToken);

        await AssertValidationProblemDetailsAsync(
            importResponse,
            expectedErrorKey: "AccountError",
            expectedErrorMessage: "Invalid account ID",
            cancellationToken);
    }

    [Fact]
    public async Task CannotImportWithUnsupportedFileExtension()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var session = await CreateStartedAppSessionAsync(cancellationToken);
        var httpClient = session.HttpClient;

        await AuthenticateAsync(httpClient, cancellationToken);
        var account = await CreateAccountAsync(httpClient, cancellationToken, "Extension Test Account", "EXT-001");

        var workbook = CreateLfWorkbookBytes(
            new ImportRow("2024-02-01", "2024-02-01", "Salary", "1000.00", "5000.00"));

        using var formData = CreateImportFormData(workbook, "invalid-extension.txt", account.Id.ToString());
        var importResponse = await httpClient.PostAsync("/api/import/upload", formData, cancellationToken);

        await AssertValidationProblemDetailsAsync(
            importResponse,
            expectedErrorKey: "ImportError",
            expectedErrorMessage: "Only Excel files (.xls, .xlsx) are supported",
            cancellationToken);
    }

    [Fact]
    public async Task CannotImportWhenWorkbookContainsNoTransactions()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var session = await CreateStartedAppSessionAsync(cancellationToken);
        var httpClient = session.HttpClient;

        await AuthenticateAsync(httpClient, cancellationToken);
        var account = await CreateAccountAsync(httpClient, cancellationToken, "Empty Workbook Account", "EMP-001");

        var workbook = CreateLfWorkbookBytes();

        using var formData = CreateImportFormData(workbook, "empty-workbook.xlsx", account.Id.ToString());
        var importResponse = await httpClient.PostAsync("/api/import/upload", formData, cancellationToken);

        await AssertValidationProblemDetailsAsync(
            importResponse,
            expectedErrorKey: "ImportError",
            expectedErrorMessage: "No transactions found in the file",
            cancellationToken);
    }

    [Fact]
    public async Task ImportDoesNotTreatDifferentBookingDateAsDuplicate()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var session = await CreateStartedAppSessionAsync(cancellationToken);
        var httpClient = session.HttpClient;

        await AuthenticateAsync(httpClient, cancellationToken);
        var account = await CreateAccountAsync(httpClient, cancellationToken, "Distinct Booking Date Account", "BOOK-001");

        var initialWorkbook = CreateLfWorkbookBytes(
            new ImportRow("2024-01-15", "2024-01-14", "Coffee Shop", "-45.67", "1000.00"));

        using var initialImportContent = CreateImportFormData(initialWorkbook, "initial-import.xlsx", account.Id.ToString());
        var initialImportResponse = await httpClient.PostAsync("/api/import/upload", initialImportContent, cancellationToken);
        Assert.True(
            initialImportResponse.IsSuccessStatusCode,
            $"Initial import failed: {await initialImportResponse.Content.ReadAsStringAsync(cancellationToken)}");

        var secondWorkbook = CreateLfWorkbookBytes(
            new ImportRow("2024-01-16", "2024-01-10", "Coffee Shop", "-45.67", "954.33"));

        using var secondImportContent = CreateImportFormData(secondWorkbook, "second-import.xlsx", account.Id.ToString());
        var secondImportResponse = await httpClient.PostAsync("/api/import/upload", secondImportContent, cancellationToken);
        Assert.True(
            secondImportResponse.IsSuccessStatusCode,
            $"Second import failed: {await secondImportResponse.Content.ReadAsStringAsync(cancellationToken)}");

        var secondImportResult = await secondImportResponse.Content.ReadFromJsonAsync<ImportResponse>(cancellationToken);
        Assert.NotNull(secondImportResult);
        Assert.True(secondImportResult.Success);
        Assert.Equal(1, secondImportResult.ImportedCount);
        Assert.Equal(0, secondImportResult.DuplicateCount);
        Assert.Empty(secondImportResult.Warnings);

        var transactionsResponse = await httpClient.GetAsync($"/api/transactions/{account.Id}", cancellationToken);
        Assert.True(
            transactionsResponse.IsSuccessStatusCode,
            $"Transaction read failed: {await transactionsResponse.Content.ReadAsStringAsync(cancellationToken)}");

        var transactions = await transactionsResponse.Content.ReadFromJsonAsync<List<TransactionListItemDto>>(cancellationToken);
        Assert.NotNull(transactions);
        Assert.Equal(2, transactions.Count);
        Assert.Contains(transactions, transaction => transaction.BookingDate == DateOnly.Parse("2024-01-15"));
        Assert.Contains(transactions, transaction => transaction.BookingDate == DateOnly.Parse("2024-01-16"));
    }

    private static async Task<TestAppSession> CreateStartedAppSessionAsync(CancellationToken cancellationToken)
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BudgetTrackerApp_AppHost>(cancellationToken);
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        var httpClient = app.CreateHttpClient("apiservice");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        return new TestAppSession(app, httpClient);
    }

    private static async Task AuthenticateAsync(HttpClient httpClient, CancellationToken cancellationToken)
    {
        var testEmail = $"test_{Guid.NewGuid()}@example.com";
        var testPassword = "Test123!";

        var registerRequest = new RegisterRequest
        {
            Email = testEmail,
            Password = testPassword,
            ConfirmPassword = testPassword,
            FirstName = "Test",
            LastName = "User"
        };

        await httpClient.PostAsJsonAsync("/api/auth/register", registerRequest, cancellationToken);

        var loginRequest = new LoginRequest
        {
            Email = testEmail,
            Password = testPassword
        };

        var loginResponse = await httpClient.PostAsJsonAsync("/api/auth/login", loginRequest, cancellationToken);
        Assert.True(
            loginResponse.IsSuccessStatusCode,
            $"Login failed: {await loginResponse.Content.ReadAsStringAsync(cancellationToken)}");

        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);
        Assert.NotNull(authResponse);

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authResponse.Token);
    }

    private static async Task<AccountDto> CreateAccountAsync(
        HttpClient httpClient,
        CancellationToken cancellationToken,
        string name,
        string accountNumber)
    {
        var createAccountRequest = new CreateAccountRequest
        {
            Name = name,
            AccountNumber = accountNumber
        };

        var createResponse = await httpClient.PostAsJsonAsync("/api/accounts", createAccountRequest, cancellationToken);
        Assert.True(
            createResponse.IsSuccessStatusCode,
            $"Account creation failed: {await createResponse.Content.ReadAsStringAsync(cancellationToken)}");

        var createdAccount = await createResponse.Content.ReadFromJsonAsync<AccountDto>(cancellationToken);
        Assert.NotNull(createdAccount);
        return createdAccount;
    }

    private static MultipartFormDataContent CreateImportFormData(byte[] fileBytes, string fileName, string accountId)
    {
        var formData = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        formData.Add(fileContent, "file", fileName);
        formData.Add(new StringContent(accountId), "accountId");
        return formData;
    }

    private static async Task AssertValidationProblemDetailsAsync(
        HttpResponseMessage response,
        string expectedErrorKey,
        string expectedErrorMessage,
        CancellationToken cancellationToken)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;

        Assert.Equal("Invalid request", root.GetProperty("title").GetString());
        Assert.Equal("Validation failed", root.GetProperty("detail").GetString());
        Assert.Equal((int)HttpStatusCode.BadRequest, root.GetProperty("status").GetInt32());

        var errorMessages = root
            .GetProperty("errors")
            .GetProperty(expectedErrorKey)
            .EnumerateArray()
            .Select(item => item.GetString())
            .Where(item => item is not null)
            .Cast<string>()
            .ToList();

        Assert.Contains(expectedErrorMessage, errorMessages);
    }

    private static byte[] CreateLfWorkbookBytes(params ImportRow[] rows)
    {
        using var stream = new MemoryStream();

        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddArchiveEntry(
                archive,
                "[Content_Types].xml",
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                </Types>
                """);
            AddArchiveEntry(
                archive,
                "_rels/.rels",
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                </Relationships>
                """);
            AddArchiveEntry(
                archive,
                "xl/workbook.xml",
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
                          xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets>
                    <sheet name="Transactions" sheetId="1" r:id="rId1"/>
                  </sheets>
                </workbook>
                """);
            AddArchiveEntry(
                archive,
                "xl/_rels/workbook.xml.rels",
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                </Relationships>
                """);
            AddArchiveEntry(archive, "xl/worksheets/sheet1.xml", BuildWorksheetXml(rows));
        }

        return stream.ToArray();
    }

    private static string BuildWorksheetXml(IEnumerable<ImportRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        builder.AppendLine("""<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">""");
        builder.AppendLine("<sheetData>");
        builder.AppendLine(BuildRowXml(1, "LF Export"));
        builder.AppendLine(BuildRowXml(2, "Booking Date", "Transaction Date", "Description", "Amount", "Balance"));

        var rowNumber = 3;
        foreach (var row in rows)
        {
            builder.AppendLine(BuildRowXml(
                rowNumber++,
                row.BookingDate,
                row.TransactionDate,
                row.Description,
                row.Amount,
                row.Balance));
        }

        builder.AppendLine("</sheetData>");
        builder.AppendLine("</worksheet>");
        return builder.ToString();
    }

    private static string BuildRowXml(int rowNumber, params string[] cellValues)
    {
        var builder = new StringBuilder();
        builder.Append($"""<row r="{rowNumber}">""");

        for (var i = 0; i < cellValues.Length; i++)
        {
            var columnLetter = (char)('A' + i);
            var escapedValue = SecurityElement.Escape(cellValues[i]) ?? string.Empty;
            builder.Append(
                $"""<c r="{columnLetter}{rowNumber}" t="inlineStr"><is><t>{escapedValue}</t></is></c>""");
        }

        builder.Append("</row>");
        return builder.ToString();
    }

    private static void AddArchiveEntry(ZipArchive archive, string entryName, string content)
    {
        var entry = archive.CreateEntry(entryName);

        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }

    private sealed class TestAppSession(IAsyncDisposable app, HttpClient httpClient) : IAsyncDisposable
    {
        public HttpClient HttpClient { get; } = httpClient;

        public ValueTask DisposeAsync() => app.DisposeAsync();
    }

    private sealed record ImportRow(
        string BookingDate,
        string TransactionDate,
        string Description,
        string Amount,
        string Balance);
}
