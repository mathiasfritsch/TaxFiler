using CsvHelper;

namespace TaxFiler.Model.Csv;

[CsvRecord]
public partial class TranactionReportDto
{
    [CsvIgnore]
    public decimal NetAmount { get; set; }

    [CsvColumn("Zahlungsbetrag", Index = 3)]
    public decimal GrossAmount { get; set; }

    [CsvColumn("UST", Index = 6)]
    public decimal TaxAmount { get; set; }

    [CsvColumn("Mwst Satz", Index = 5)]
    public decimal TaxRate { get; set; }

    [CsvColumn("Transaktionsreferenz", Index = 7)]
    public required string TransactionReference { get; set; }

    [CsvColumn("Buchungsdatum", Index = 0)]
    [CsvFormat("yyyy-MM-dd")]
    public DateTime TransactionDateTime { get; set; }

    [CsvColumn("Beschreibung", Index = 2)]
    public required string TransactionNote { get; set; }

    [CsvColumn("Ausgehend")]
    public bool IsOutgoing { get; set; }

    [CsvColumn("Ust relevant")]
    public bool IsIncomeTaxRelevant { get; set; }

    [CsvColumn("Einkommenssteuer relevant")]
    public bool IsSalesTaxRelevant { get; set; }

    [CsvColumn("Begleitende Dokumente", Index = 4)]
    public required string DocumentName { get; set; }

    [CsvColumn("Auftraggeber/Empfaenger", Index = 1)]
    public required string SenderReceiver { get; set; }
}
