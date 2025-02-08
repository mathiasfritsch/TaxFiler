using CsvHelper.Configuration.Attributes;

namespace TaxFiler.Model.Csv;

public class TranactionReportDto
{
    [Name("Nettobetrag")]
    [Ignore]
    public decimal NetAmount { get; set; }
    
    [Index(3)]
    [Name("Zahlungsbetrag")]
    public decimal GrossAmount { get; set; }
    
    [Index(6)]
    [Name("UST")]
    public decimal TaxAmount { get; set; }
    
    [Index(5)]
    [Name("Mwst Satz")]
    public decimal TaxRate { get; set; }
    
    [Index(7)]
    [Name("Transaktionsreferenz")]
    public required string TransactionReference { get; set; }
    
    [Index(0)]
    [Name("Buchungsdatum")]
    [Format("yyyy-MM-dd")]
    public DateTime TransactionDateTime { get; set; }
    
    [Index(2)]
    [Name("Beschreibung")]
    public required string TransactionNote { get; set; }
    
    [Name("Ausgehend")]
    
    public bool IsOutgoing { get; set; }
    [Name("Ust relevant")]
    
    public bool IsIncomeTaxRelevant { get; set; }
    [Name("Einkommenssteuer relevant")]
    public bool IsSalesTaxRelevant { get; set; }
    
    [Index(4)]
    [Name("Begleitende Dokumente")]
    public required string DocumentName { get; set; }
    
    [Index(1)]
    [Name("Auftraggeber/Empfaenger")]
    public required string SenderReceiver { get; set; }
}