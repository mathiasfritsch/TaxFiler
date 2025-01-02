using CsvHelper.Configuration.Attributes;


namespace TaxFiler.Model.Csv;

public class TranactionReportDto
{
    [Name("Nettobetrag")]
    public decimal NetAmount { get; set; }
    [Name("Zahlungsbetrag")]
    public decimal GrossAmount { get; set; }
    [Name("UST")]
    public decimal TaxAmount { get; set; }
    [Name("Mwst Satz")]
    public decimal TaxRate { get; set; }
    [Name("Transaktionsreferenz")]
    public string TransactionReference { get; set; }
    [Name("Buchungsdatum")]
    [Format("dd.MM.yyyy")]
    public DateTime TransactionDateTime { get; set; }
    [Name("Beschreibung")]
    public string TransactionNote { get; set; }
    [Name("Ausgehend")]
    public bool IsOutgoing { get; set; }
    [Name("Ust relevant")]
    public bool IsIncomeTaxRelevant { get; set; }
    [Name("Einkommenssteuer relevant")]
    public bool IsSalesTaxRelevant { get; set; }
    [Name("Begleitende Dokumente")]
    public string DocumentName { get; set; }
    [Name("Auftraggeber/Empfänger")]
    public string SenderReceiver { get; set; }
}