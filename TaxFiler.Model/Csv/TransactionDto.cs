using CsvHelper.Configuration.Attributes;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
namespace TaxFiler.Model.Csv;

public class TransactionDto
{
    [Name("Buchungstag")]
    [Format("dd.MM.yyyy")]
    public DateTime BookingDate { get; init; }
    
    [Name("Name Zahlungsbeteiligter")]
    public string SenderReceiver { get; init; }
    
    [Name("BIC (SWIFT-Code) Zahlungsbeteiligter")]
    public string CounterPartyBIC { get; init; }
    
    [Name("IBAN Zahlungsbeteiligter")]
    public string CounterPartyIBAN { get; init; }
    
    [Name("Verwendungszweck")]
    public string Comment { get; init; }
    
    [Name("Betrag")]
    public decimal Amount { get; init; }
}