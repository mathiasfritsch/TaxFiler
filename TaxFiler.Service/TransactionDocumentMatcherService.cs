using System.Collections.Immutable;
using TaxFiler.DB;
using TaxFiler.Model.Dto;
using TransactionDto = TaxFiler.Model.Csv.TransactionDto;

namespace TaxFiler.Service;
public class TransactionDocumentMatcherService(TaxFilerContext context):ITransactionDocumentMatcherService
{
    public DocumentDto[] MatchTransactionToDocument(TransactionDto transaction)
    {
        var transactionDocumentMatchers = context
            .TransactionDocumentMatchers
            .Where( m=> m.TransactionReceiver == transaction.SenderReceiver)
            .ToImmutableArray();
        
        var matchedDocuments = context
            .Transactions
            .Where(t => t.DocumentId != null)
            .Select( t => t.DocumentId)
            .Distinct()
            .ToArray();
            
        var documentsUnmatched = context
            .Documents
            .Where(d => !matchedDocuments.Contains(d.Id))
            .Where(d => d.Parsed && d.InvoiceNumber != null);
        
        foreach (var transactionDocumentMatcher in transactionDocumentMatchers)
        {
            
            
        }
            

        throw new NotImplementedException();
    }
}