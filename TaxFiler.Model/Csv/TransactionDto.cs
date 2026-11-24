using CsvHelper;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
namespace TaxFiler.Model.Csv;

[CsvRecord]
public partial class TransactionDto
{
    [CsvColumn("Buchungstag")]
    [CsvFormat("dd.MM.yyyy")]
    public DateTime BookingDate { get; init; }

    [CsvColumn("Name Zahlungsbeteiligter")]
    public string SenderReceiver { get; init; }

    [CsvColumn("BIC (SWIFT-Code) Zahlungsbeteiligter")]
    public string CounterPartyBIC { get; init; }

    [CsvColumn("IBAN Zahlungsbeteiligter")]
    public string CounterPartyIBAN { get; init; }

    [CsvColumn("Verwendungszweck")]
    public string Comment { get; init; }

    [CsvColumn("Betrag")]
    public decimal Amount { get; init; }
}
