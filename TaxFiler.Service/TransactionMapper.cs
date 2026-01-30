using TaxFiler.DB.Model;
using TaxFiler.Model.Dto;

namespace TaxFiler.Service;

public static class TransactionMapper
{
    public static Transaction ToTransaction(this TaxFiler.Model.Csv.TransactionDto transaction) =>
        new()
        {
            AccountId = 1,
            GrossAmount = transaction.Amount,
            SenderReceiver = transaction.SenderReceiver,
            Counterparty = transaction.CounterPartyIBAN,
            TransactionNote = transaction.Comment,
            TransactionDateTime = DateTime.SpecifyKind(transaction.BookingDate, DateTimeKind.Utc),
            IsOutgoing = transaction.Amount < 0,
            IsIncomeTaxRelevant = false,
            IsSalesTaxRelevant = false
        };
    
    public static TransactionDto TransactionDto(this Transaction transaction) =>
        new()
        {
            Id = transaction.Id,
            NetAmount = transaction.NetAmount??0,
            GrossAmount = transaction.GrossAmount,
            TaxAmount = transaction.TaxAmount??0,
            TaxRate = transaction.TaxRate??0,
            Counterparty = transaction.Counterparty,
            TransactionNote = transaction.TransactionNote,
            TransactionReference = transaction.TransactionReference,
            TransactionDateTime = transaction.TransactionDateTime,
            IsSalesTaxRelevant = transaction.IsSalesTaxRelevant??false,
            IsOutgoing = transaction.IsOutgoing,
            IsIncomeTaxRelevant = transaction.IsIncomeTaxRelevant??false,
            DocumentId = transaction.DocumentId,
            Document = transaction.Document?.ToDto([]),
            Documents = transaction.TransactionDocuments?
                .Select(td => td.Document.ToDto([]))
                .ToList() ?? new List<Model.Dto.DocumentDto>(),
            SenderReceiver = transaction.SenderReceiver,
            AccountId = transaction.AccountId,
            AccountName = transaction.Account.Name,
            IsTaxMismatch = CalculateTaxMismatch(transaction)
        };
    
    /// <summary>
    /// Calculates whether the tax amount matches the expected value based on the tax rate and gross amount.
    /// Returns true if there's a mismatch (i.e., the tax calculation is incorrect).
    /// </summary>
    private static bool CalculateTaxMismatch(Transaction transaction)
    {
        // Skip validation if any required values are missing or zero
        if (!transaction.TaxAmount.HasValue || transaction.TaxAmount.Value == 0 ||
            !transaction.TaxRate.HasValue || transaction.TaxRate.Value == 0 ||
            transaction.GrossAmount == 0)
        {
            return false;
        }

        var taxRate = transaction.TaxRate.Value;
        var taxAmount = transaction.TaxAmount.Value;
        var grossAmount = transaction.GrossAmount;

        // Calculate expected tax amount from gross amount and tax rate
        // Formula: TaxAmount = GrossAmount * TaxRate / (100 + TaxRate)
        // This is because GrossAmount = NetAmount + TaxAmount and TaxAmount = NetAmount * TaxRate / 100
        var expectedTaxAmount = grossAmount * taxRate / (100 + taxRate);
        
        // Use a small tolerance (0.02) for rounding differences
        var tolerance = 0.02m;
        var difference = Math.Abs(taxAmount - expectedTaxAmount);
        
        return difference > tolerance;
    }

    public static void UpdateTransaction( Transaction transaction, UpdateTransactionDto transactionDto)
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
        transaction.DocumentId = transactionDto.DocumentId > 0 ? transactionDto.DocumentId : null;
        transaction.SenderReceiver = transactionDto.SenderReceiver;
        transaction.AccountId = transactionDto.AccountId ?? transaction.AccountId;
    }
}