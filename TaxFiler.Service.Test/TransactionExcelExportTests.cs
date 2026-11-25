using ClosedXML.Excel;
using NSubstitute;
using NUnit.Framework;
using TaxFiler.DB;
using TaxFiler.DB.Model;

namespace TaxFiler.Service.Test;

/// <summary>
/// Tests for the USt-Voranmeldung Excel export that mirrors the manually
/// maintained "Umsätze" workbook layout.
/// </summary>
[TestFixture]
public class TransactionExcelExportTests
{
    private TestTaxFilerContext _context;
    private TransactionService _service;

    [SetUp]
    public void Setup()
    {
        _context = new TestTaxFilerContext();
        _service = new TransactionService(_context, Substitute.For<IDocumentMatchingService>());
    }

    [TearDown]
    public void TearDown() => _context?.Dispose();

    [Test]
    public async Task CreateExcelFileAsync_BuildsSectionsWithHeadersSumsAndFormats()
    {
        var yearMonth = new DateOnly(2026, 10, 1);
        var account = new Account { Name = "Geschäftskonto" };
        await _context.Accounts.AddAsync(account);

        // Money out of the account -> IsOutgoing = true -> "Eingangsrechnungen".
        var incoming = new Transaction
        {
            Account = account,
            GrossAmount = 59.5m,
            NetAmount = 50m,
            TaxAmount = 9.5m,
            TaxRate = 19m,
            TransactionReference = "REF-IN",
            TransactionDateTime = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc),
            TransactionNote = "Steuerberaterin Rechnung",
            SenderReceiver = "Martina Ibendorf",
            IsOutgoing = true,
            IsIncomeTaxRelevant = true,
            IsSalesTaxRelevant = true,
            Counterparty = "IBAN-IN"
        };

        // Money into the account -> IsOutgoing = false -> "Ausgangsrechnungen".
        var outgoing = new Transaction
        {
            Account = account,
            GrossAmount = 5335.17m,
            NetAmount = 4483.34m,
            TaxAmount = 851.83m,
            TaxRate = 19m,
            TransactionReference = "REF-OUT",
            TransactionDateTime = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc),
            TransactionNote = "Rechnung Hays",
            SenderReceiver = "Hays AG",
            IsOutgoing = false,
            IsIncomeTaxRelevant = true,
            IsSalesTaxRelevant = true,
            Counterparty = "IBAN-OUT"
        };

        await _context.Transactions.AddRangeAsync(incoming, outgoing);
        await _context.SaveChangesAsync();

        using var stream = await _service.CreateExcelFileAsync(yearMonth);
        using var workbook = new XLWorkbook(stream);

        var sheet = workbook.Worksheet("Zahlungen");
        Assert.That(sheet, Is.Not.Null);

        // Incoming section.
        Assert.That(sheet.Cell(1, 1).GetString(), Is.EqualTo("Eingangsrechnungen"));
        Assert.That(sheet.Cell(2, 1).GetString(), Is.EqualTo("Buchungsdatum"));
        Assert.That(sheet.Cell(2, 1).Style.Font.Bold, Is.True);
        Assert.That(sheet.Cell(2, 10).GetString(), Is.EqualTo("Konto"));

        Assert.That(sheet.Cell(3, 2).GetString(), Is.EqualTo("Martina Ibendorf"));
        Assert.That(sheet.Cell(3, 4).GetDouble(), Is.EqualTo(59.5).Within(0.0001));
        Assert.That(sheet.Cell(3, 7).GetDouble(), Is.EqualTo(9.5).Within(0.0001));
        Assert.That(sheet.Cell(3, 10).GetString(), Is.EqualTo("Geschäftskonto"));
        Assert.That(sheet.Cell(3, 1).GetDateTime().Date, Is.EqualTo(new DateTime(2026, 10, 15)));
        Assert.That(sheet.Cell(3, 1).Style.NumberFormat.Format, Is.EqualTo("mm-dd-yy"));

        // Incoming sum row.
        Assert.That(sheet.Cell(4, 6).GetString(), Is.EqualTo("Summe"));
        Assert.That(sheet.Cell(4, 7).FormulaA1, Is.EqualTo("SUM(G3:G3)"));
        Assert.That(sheet.Cell(4, 7).Style.NumberFormat.Format, Does.Contain("€"));

        // Outgoing section.
        Assert.That(sheet.Cell(5, 1).GetString(), Is.EqualTo("Ausgangsrechnungen / Gutschriften"));
        Assert.That(sheet.Cell(6, 1).GetString(), Is.EqualTo("Buchungsdatum"));
        Assert.That(sheet.Cell(7, 2).GetString(), Is.EqualTo("Hays AG"));
        Assert.That(sheet.Cell(7, 4).GetDouble(), Is.EqualTo(5335.17).Within(0.0001));

        // Outgoing sum row.
        Assert.That(sheet.Cell(8, 6).GetString(), Is.EqualTo("Summe"));
        Assert.That(sheet.Cell(8, 7).FormulaA1, Is.EqualTo("SUM(G7:G7)"));
    }
}
