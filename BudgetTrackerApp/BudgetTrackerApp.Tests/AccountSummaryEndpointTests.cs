using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IO.Compression;
using System.Security;
using System.Text;
using BudgetTrackerApp.ApiService.DTOs;
using Microsoft.Extensions.Logging;

namespace BudgetTrackerApp.Tests;

public class AccountSummaryEndpointTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task CanReadSummaryForAccessibleAccount()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

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

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        var httpClient = app.CreateHttpClient("apiservice");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        var token = await RegisterAndLoginAsync(httpClient, cancellationToken);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var accountId = await CreateAccountAsync(httpClient, "Visible account", "ACC-001", cancellationToken);
        await ImportTransactionsAsync(httpClient, accountId, new[]
        {
            new TransactionImportRow(new DateOnly(2026, 3, 5), new DateOnly(2026, 3, 6), "Latest transaction", -50.25m, 949.75m),
            new TransactionImportRow(new DateOnly(2026, 3, 4), new DateOnly(2026, 3, 4), "Earlier transaction", 100m, 1000m)
        }, cancellationToken);

        var response = await httpClient.GetAsync($"/api/transactions/{accountId}/summary", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<AccountSummaryDto>(cancellationToken);
        Assert.NotNull(summary);
        Assert.Equal(949.75m, summary.CurrentBalance);
        Assert.Equal(new DateOnly(2026, 3, 6), summary.LastUpdatedDate);
        Assert.Equal(2, summary.TransactionCount);
    }

    [Fact]
    public async Task CannotReadSummaryForInaccessibleAccount()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

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

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        var httpClient = app.CreateHttpClient("apiservice");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        var token = await RegisterAndLoginAsync(httpClient, cancellationToken);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var otherUserClient = app.CreateHttpClient("apiservice");
        var otherToken = await RegisterAndLoginAsync(otherUserClient, cancellationToken);
        otherUserClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);

        var inaccessibleAccountId = await CreateAccountAsync(otherUserClient, "Private account", "ACC-999", cancellationToken);
        await ImportTransactionsAsync(otherUserClient, inaccessibleAccountId, new[]
        {
            new TransactionImportRow(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 2), "Hidden transaction", 42m, 42m)
        }, cancellationToken);

        var response = await httpClient.GetAsync($"/api/transactions/{inaccessibleAccountId}/summary", cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CannotReadSummaryWithoutAuthentication()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

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

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        var httpClient = app.CreateHttpClient("apiservice");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        var response = await httpClient.GetAsync("/api/transactions/1/summary", cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<string> RegisterAndLoginAsync(HttpClient httpClient, CancellationToken cancellationToken)
    {
        var testEmail = $"test_{Guid.NewGuid()}@example.com";
        var testPassword = "Test123!";

        var registerRequest = new RegisterRequest
        {
            Email = testEmail,
            Password = testPassword,
            ConfirmPassword = testPassword
        };

        var registerResponse = await httpClient.PostAsJsonAsync("/api/auth/register", registerRequest, cancellationToken);
        Assert.True(registerResponse.IsSuccessStatusCode, $"Registration failed: {await registerResponse.Content.ReadAsStringAsync(cancellationToken)}");

        var loginRequest = new LoginRequest
        {
            Email = testEmail,
            Password = testPassword
        };

        var loginResponse = await httpClient.PostAsJsonAsync("/api/auth/login", loginRequest, cancellationToken);
        Assert.True(loginResponse.IsSuccessStatusCode, $"Login failed: {await loginResponse.Content.ReadAsStringAsync(cancellationToken)}");

        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);
        Assert.NotNull(authResponse);

        return authResponse.Token;
    }

    private static async Task<int> CreateAccountAsync(HttpClient httpClient, string name, string accountNumber, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync("/api/accounts", new CreateAccountRequest
        {
            Name = name,
            AccountNumber = accountNumber
        }, cancellationToken);

        Assert.True(response.IsSuccessStatusCode, $"Account creation failed: {await response.Content.ReadAsStringAsync(cancellationToken)}");

        var account = await response.Content.ReadFromJsonAsync<AccountDto>(cancellationToken);
        Assert.NotNull(account);

        return account.Id;
    }

    private static async Task ImportTransactionsAsync(
        HttpClient httpClient,
        int accountId,
        IReadOnlyList<TransactionImportRow> transactions,
        CancellationToken cancellationToken)
    {
        using var formData = new MultipartFormDataContent();
        formData.Add(new StringContent(accountId.ToString()), "accountId");

        var fileContent = new ByteArrayContent(CreateImportWorkbook(transactions));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        formData.Add(fileContent, "file", "transactions.xlsx");

        var response = await httpClient.PostAsync("/api/import/upload", formData, cancellationToken);
        Assert.True(response.IsSuccessStatusCode, $"Import failed: {await response.Content.ReadAsStringAsync(cancellationToken)}");
    }

    private static byte[] CreateImportWorkbook(IReadOnlyList<TransactionImportRow> transactions)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteZipEntry(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                </Types>
                """);

            WriteZipEntry(archive, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                </Relationships>
                """);

            WriteZipEntry(archive, "xl/workbook.xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
                          xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets>
                    <sheet name="Transactions" sheetId="1" r:id="rId1"/>
                  </sheets>
                </workbook>
                """);

            WriteZipEntry(archive, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                </Relationships>
                """);

            WriteZipEntry(archive, "xl/worksheets/sheet1.xml", BuildWorksheetXml(transactions));
        }

        return stream.ToArray();
    }

    private static string BuildWorksheetXml(IReadOnlyList<TransactionImportRow> transactions)
    {
        var rows = new List<string>
        {
            BuildRow(1, ["Transactions", "", "", "", ""]),
            BuildRow(2, ["Booking Date", "Transaction Date", "Description", "Amount", "Balance"])
        };

        for (var i = 0; i < transactions.Count; i++)
        {
            var transaction = transactions[i];
            rows.Add(BuildRow(i + 3, [
                transaction.BookingDate.ToString("yyyy-MM-dd"),
                transaction.TransactionDate.ToString("yyyy-MM-dd"),
                transaction.Description,
                transaction.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                transaction.Balance.ToString(System.Globalization.CultureInfo.InvariantCulture)
            ]));
        }

        return $$"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
              <sheetData>
                {{string.Join(Environment.NewLine, rows)}}
              </sheetData>
            </worksheet>
            """;
    }

    private static string BuildRow(int rowIndex, IReadOnlyList<string> values)
    {
        var cells = values.Select((value, index) => BuildCell($"{GetColumnName(index + 1)}{rowIndex}", value));
        return $"<row r=\"{rowIndex}\">{string.Join(string.Empty, cells)}</row>";
    }

    private static string BuildCell(string cellReference, string value)
    {
        var escapedValue = SecurityElement.Escape(value) ?? string.Empty;
        return $"<c r=\"{cellReference}\" t=\"inlineStr\"><is><t>{escapedValue}</t></is></c>";
    }

    private static string GetColumnName(int columnNumber)
    {
        var builder = new StringBuilder();

        while (columnNumber > 0)
        {
            columnNumber--;
            builder.Insert(0, (char)('A' + (columnNumber % 26)));
            columnNumber /= 26;
        }

        return builder.ToString();
    }

    private static void WriteZipEntry(ZipArchive archive, string entryName, string content)
    {
        var entry = archive.CreateEntry(entryName);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content.Trim());
    }

    private sealed record TransactionImportRow(
        DateOnly BookingDate,
        DateOnly TransactionDate,
        string Description,
        decimal Amount,
        decimal Balance);
}
