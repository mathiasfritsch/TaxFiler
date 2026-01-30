using TaxFiler.Model.Dto;
using TaxFiler.Models;

namespace TaxFiler.Mapper;

public static class TransactionMapper
{
    public static TransactionViewModel ToViewModel(this TransactionDto transaction)
    {
        return new TransactionViewModel
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
            DocumentId = transaction.DocumentId,
            DocumentName = transaction.Document?.Name,
            DocumentNames = transaction.Documents?.Select(d => d.Name).ToList() ?? new List<string>(),
            SenderReceiver = transaction.SenderReceiver,
            AccountId = transaction.AccountId,
            AccountName = transaction.AccountName,
            IsTaxMismatch = transaction.IsTaxMismatch
        };
    }
}