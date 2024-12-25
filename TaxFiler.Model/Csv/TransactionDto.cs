using CsvHelper.Configuration.Attributes;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
namespace TaxFiler.Model.Csv;

public class TransactionDto
{
    [Name("Transaktions-ID")]
    public string TransactionID { get; init; }
    
    [Name("Buchungsdatum")]
    [Format("dd.MM.yyyy")]
    public DateOnly BookingDate { get; init; }
    
    [Name("Time completed")]
    public TimeOnly TimeCompleted { get; init; }
    
    [Name("Status")]
    public string State{get;init;}
    
    [Name("Transaktionsart")]
    public string TransactionKind { get; init; }
    
    [Name("Auftraggeber/Empfänger")]
    public string SenderReceiver { get; init; }
    
    [Name("Counterparty BIC")]
    public string CounterPartyBIC { get; init; }
    
    [Name("Counterparty IBAN")]
    public string CounterPartyIBAN { get; init; }
    
    [Name("Verwendungszweck")]
    public string Comment { get; init; }
    
    [Name("Zahlungsbetrag")]
    public decimal Amount { get; init; }
    
    [Name("Wallet-Name")]
    public string Wallet { get; init; }
}