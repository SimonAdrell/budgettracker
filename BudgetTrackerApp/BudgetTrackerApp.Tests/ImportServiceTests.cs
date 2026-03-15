using System.IO.Compression;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using BudgetTrackerApp.ApiService.Data;
using BudgetTrackerApp.ApiService.DTOs;
using BudgetTrackerApp.ApiService.Models;
using BudgetTrackerApp.ApiService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BudgetTrackerApp.Tests;

public sealed class ImportServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IAccountService> _accountServiceMock;
    private readonly Mock<ISnapshotService> _snapshotServiceMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IServiceGuard> _serviceGuardMock;
    private readonly ImportService _service;

    public ImportServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _accountServiceMock = new Mock<IAccountService>();
        _snapshotServiceMock = new Mock<ISnapshotService>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _serviceGuardMock = new Mock<IServiceGuard>();

        _serviceGuardMock.Setup(g => g.GetValidUser()).Returns("test-user");
        _accountServiceMock
            .Setup(service => service.UserHasAccessToAccountAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _snapshotServiceMock
            .Setup(service => service.GenerateSnapshotsAsync(
                It.IsAny<int>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResponse<int>.Success(0));
        _snapshotServiceMock
            .Setup(service => service.GenerateSnapshotsForAllTransactionsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResponse<int>.Success(0));

        _service = new ImportService(
            _context,
            _accountServiceMock.Object,
            _snapshotServiceMock.Object,
            _httpContextAccessorMock.Object,
            _serviceGuardMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task ImportTransactionsFromExcelAsync_BackfilledTransaction_RegeneratesOnlyAffectedTransactionDateRange()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        const int accountId = 42;
        var earliestImportedTransactionDate = new DateOnly(2024, 1, 15);
        var importedBookingDate = new DateOnly(2024, 1, 16);
        var latestAffectedTransactionDate = new DateOnly(2024, 1, 20);

        _context.Accounts.Add(new Account
        {
            Id = accountId,
            Name = "Snapshot Range Account"
        });
        _context.Transactions.AddRange(
            new Transaction
            {
                AccountId = accountId,
                BookingDate = new DateOnly(2024, 1, 10),
                TransactionDate = new DateOnly(2024, 1, 10),
                Description = "Opening balance",
                Amount = 100.00m,
                Balance = 100.00m
            },
            new Transaction
            {
                AccountId = accountId,
                BookingDate = new DateOnly(2024, 1, 20),
                TransactionDate = latestAffectedTransactionDate,
                Description = "Later transaction",
                Amount = 25.00m,
                Balance = 175.00m
            });
        await _context.SaveChangesAsync(cancellationToken);

        var workbook = CreateLfWorkbookBytes(
            new ImportRow(
                BookingDate: importedBookingDate.ToString("yyyy-MM-dd"),
                TransactionDate: earliestImportedTransactionDate.ToString("yyyy-MM-dd"),
                Description: "Backfilled transaction",
                Amount: "50.00",
                Balance: "150.00"));

        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<IFormFeature>(new FormFeature(new FormCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["accountId"] = accountId.ToString()
            },
            new FormFileCollection
            {
                CreateFormFile(workbook, "backfill.xlsx")
            })));
        _httpContextAccessorMock.Setup(accessor => accessor.HttpContext).Returns(httpContext);

        var response = await _service.ImportTransactionsFromExcelAsync(cancellationToken);

        Assert.Equal(ServiceResponseType.Success, response.ResponseType);
        Assert.NotNull(response.Data);
        Assert.True(response.Data.Success);
        Assert.Equal(1, response.Data.ImportedCount);

        _snapshotServiceMock.Verify(service => service.GenerateSnapshotsAsync(
            accountId,
            earliestImportedTransactionDate,
            latestAffectedTransactionDate,
            cancellationToken), Times.Once);
        _snapshotServiceMock.Verify(service =>
            service.GenerateSnapshotsForAllTransactionsAsync(accountId, cancellationToken), Times.Never);
    }

    private static IFormFile CreateFormFile(byte[] fileBytes, string fileName)
    {
        var stream = new MemoryStream(fileBytes);
        var formFile = new FormFile(stream, 0, fileBytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        formFile.Headers.ContentType = new MediaTypeHeaderValue(formFile.ContentType).ToString();
        return formFile;
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
            AddArchiveEntry(archive, "xl/worksheets/sheet1.xml", BuildSheetXml(rows));
        }

        return stream.ToArray();
    }

    private static string BuildSheetXml(IEnumerable<ImportRow> rows)
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

    private static void AddArchiveEntry(ZipArchive archive, string path, string contents)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(contents.Trim());
    }

    private sealed record ImportRow(
        string BookingDate,
        string TransactionDate,
        string Description,
        string Amount,
        string Balance);
}
