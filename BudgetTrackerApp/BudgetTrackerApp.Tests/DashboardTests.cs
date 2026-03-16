using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security;
using System.Text;
using BudgetTrackerApp.ApiService.DTOs;
using Microsoft.Extensions.Logging;

namespace BudgetTrackerApp.Tests;

public class DashboardTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task CannotReadDashboardWithoutAuthentication()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var session = await CreateStartedAppSessionAsync(cancellationToken);

        var response = await session.HttpClient.GetAsync("/api/Dashboard/1", cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CanReadDashboardForEmptyAccount()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var session = await CreateStartedAppSessionAsync(cancellationToken);
        var httpClient = session.HttpClient;

        await AuthenticateAsync(httpClient, cancellationToken);
        var account = await CreateAccountAsync(httpClient, cancellationToken, "Empty account", "EMPTY-001");

        var response = await httpClient.GetAsync($"/api/Dashboard/{account.Id}", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dashboard = await response.Content.ReadFromJsonAsync<AccountDashboardDto>(cancellationToken);
        Assert.NotNull(dashboard);
        Assert.Equal(account.Id, dashboard.AccountId);
        Assert.Equal(account.Name, dashboard.AccountName);
        Assert.Equal(0m, dashboard.CurrentBalance);
        Assert.Null(dashboard.LastUpdated);
        Assert.Equal(0, dashboard.TransactionCount);
        Assert.False(dashboard.HasTransactions);
        Assert.Empty(dashboard.RecentTransactions);
    }

    [Fact]
    public async Task CanReadDashboardForImportedTransactions()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var session = await CreateStartedAppSessionAsync(cancellationToken);
        var httpClient = session.HttpClient;

        await AuthenticateAsync(httpClient, cancellationToken);
        var account = await CreateAccountAsync(httpClient, cancellationToken, "Primary account", "DASH-001");

        var workbook = CreateLfWorkbookBytes(
            new ImportRow("2026-01-04", "2026-01-04", "Salary", "1000.00", "1000.00"),
            new ImportRow("2026-01-08", "2026-01-08", "Groceries", "-24.50", "975.50"),
            new ImportRow("2026-01-08", "2026-01-09", "Coffee Shop", "-25.75", "949.75"));

        using var importContent = CreateImportFormData(workbook, "dashboard-import.xlsx", account.Id.ToString());
        var importResponse = await httpClient.PostAsync("/api/import/upload", importContent, cancellationToken);
        Assert.True(
            importResponse.IsSuccessStatusCode,
            $"Import failed: {await importResponse.Content.ReadAsStringAsync(cancellationToken)}");

        var response = await httpClient.GetAsync($"/api/Dashboard/{account.Id}", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dashboard = await response.Content.ReadFromJsonAsync<AccountDashboardDto>(cancellationToken);
        Assert.NotNull(dashboard);
        Assert.Equal(account.Id, dashboard.AccountId);
        Assert.Equal(account.Name, dashboard.AccountName);
        Assert.Equal(949.75m, dashboard.CurrentBalance);
        Assert.Equal(DateOnly.Parse("2026-01-09"), dashboard.LastUpdated);
        Assert.Equal(3, dashboard.TransactionCount);
        Assert.True(dashboard.HasTransactions);

        Assert.Equal(3, dashboard.RecentTransactions.Count);

        Assert.Equal(DateOnly.Parse("2026-01-09"), dashboard.RecentTransactions[0].Date);
        Assert.Equal("Coffee Shop", dashboard.RecentTransactions[0].Description);
        Assert.Equal(-25.75m, dashboard.RecentTransactions[0].Amount);
        Assert.Equal(949.75m, dashboard.RecentTransactions[0].Balance);

        Assert.Equal(DateOnly.Parse("2026-01-08"), dashboard.RecentTransactions[1].Date);
        Assert.Equal("Groceries", dashboard.RecentTransactions[1].Description);
        Assert.Equal(-24.50m, dashboard.RecentTransactions[1].Amount);
        Assert.Equal(975.50m, dashboard.RecentTransactions[1].Balance);

        Assert.Equal(DateOnly.Parse("2026-01-04"), dashboard.RecentTransactions[2].Date);
        Assert.Equal("Salary", dashboard.RecentTransactions[2].Description);
        Assert.Equal(1000.00m, dashboard.RecentTransactions[2].Amount);
        Assert.Equal(1000.00m, dashboard.RecentTransactions[2].Balance);
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
