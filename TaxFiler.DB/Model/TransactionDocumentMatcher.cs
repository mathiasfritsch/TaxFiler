using System.ComponentModel.DataAnnotations;

namespace TaxFiler.DB.Model;

public class TransactionDocumentMatcher
{
    [Key]
    public int Id { get; set; }
    
    public required string TransactionReceiver { get; init; }
    
    public required string TransactionCommentPattern { get; init; }
    
    public bool AmountMatches { get; set; }
}
