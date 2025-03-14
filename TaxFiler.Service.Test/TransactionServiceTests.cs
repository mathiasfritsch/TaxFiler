using System.Text;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using TaxFiler.DB;

namespace TaxFiler.Service.Test;

[TestFixture]
public class TransactionServiceTests
{
    [Test]
    public void Upload_ShouldProcessUploadedFile()
    {
        // Arrange
        var csvContent =
            "Bezeichnung Auftragskonto;IBAN Auftragskonto;BIC Auftragskonto;Bankname Auftragskonto;Buchungstag;Valutadatum;Name Zahlungsbeteiligter;IBAN Zahlungsbeteiligter;BIC (SWIFT-Code) Zahlungsbeteiligter;Buchungstext;Verwendungszweck;Betrag;Waehrung;Saldo nach Buchung;Bemerkung;Kategorie;Steuerrelevant;Glaeubiger ID;Mandatsreferenz\n\n" +
            "Konto;someiban;somebic;some bank;14.03.2025;14.03.2025;some name;some iban;some bic;some type;some note;-66,85;EUR;9580,27;;;;;\n";
        var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));
        var taxFilerContext = new TaxFilerContext(Substitute.For<IConfiguration>());

        var transactionService = new TransactionService(taxFilerContext);

        // Act

        using var reader = new StreamReader(fileStream);

        var transactions = transactionService.ParseTransactions(reader).ToList();

        // Assert

        Assert.That(transactions.Count, Is.EqualTo(1));
    }
}