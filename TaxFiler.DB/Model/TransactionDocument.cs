using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxFiler.DB.Model;

/// <summary>
/// Junction table for many-to-many relationship between Transaction and Document
/// </summary>
public class TransactionDocument
{
    [ForeignKey(nameof(Transaction))]
    public int TransactionId { get; set; }
    public Transaction Transaction { get; set; }
    
    [ForeignKey(nameof(Document))]
    public int DocumentId { get; set; }
    public Document Document { get; set; }
}
