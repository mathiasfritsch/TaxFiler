using TaxFiler.DB.Model;
using TaxFiler.Model.Dto;

namespace TaxFiler.Service;

public static class TransactionMapper
{
    public static Transaction ToTransaction(this TaxFiler.Model.Csv.TransactionDto transaction) =>
        new()
        {
            GrossAmount = transaction.Amount,
            Counterparty = transaction.CounterPartyIBAN,
            TransactionNote = transaction.Comment,
            TransactionReference = transaction.TransactionID,
            TransactionDateTime = new DateTime(transaction.BookingDate, transaction.TimeCompleted),
            IsOutgoing = transaction.Amount < 0,
            IsIncomeTaxRelevant = false,
            IsSalesTaxRelevant = false
        };
    
    public static TransactionDto TransactionDto(this Transaction transaction) =>
        new()
        {
            Id = transaction.Id,
            NetAmount = transaction.NetAmount,
            GrossAmount = transaction.GrossAmount,
            TaxAmount = transaction.TaxAmount,
            TaxRate = transaction.TaxRate,
            Counterparty = transaction.Counterparty,
            TransactionNote = transaction.TransactionNote,
            TransactionReference = transaction.TransactionReference,
            TransactionDateTime = transaction.TransactionDateTime,
            IsSalesTaxRelevant = transaction.IsSalesTaxRelevant,
            IsOutgoing = transaction.IsOutgoing,
            IsIncomeTaxRelevant = transaction.IsIncomeTaxRelevant,
            TaxMonth = transaction.TaxMonth,
            TaxYear = transaction.TaxYear,
            DocumentId = transaction.DocumentId
        };

    public static void UpdateTransaction( Transaction transaction, TransactionDto transactionDto)
    {
        transaction.IsIncomeTaxRelevant = transactionDto.IsIncomeTaxRelevant;
        transaction.IsOutgoing  = transactionDto.IsOutgoing;
        transaction.IsSalesTaxRelevant = transactionDto.IsSalesTaxRelevant;
        transaction.NetAmount = transactionDto.NetAmount;
        transaction.TaxAmount = transactionDto.TaxAmount;
        transaction.TaxRate = transactionDto.TaxRate;
        transaction.TransactionDateTime = transactionDto.TransactionDateTime;
        transaction.TransactionNote = transactionDto.TransactionNote;
        transaction.TransactionReference = transactionDto.TransactionReference;
        transaction.Counterparty = transactionDto.Counterparty;
    }
}