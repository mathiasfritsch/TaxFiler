using System.ComponentModel.DataAnnotations.Schema;

namespace TaxFiler.DB.Model;

public class Booking
{
    public int Id { get; set; }
    
    [ForeignKey(nameof(Document))]
    public int? DocumnentId { get; set; }
    [ForeignKey(nameof(Transaction))]
    public int? TransactionId { get; set; }
    
    public Transaction Transaction { get; set; }
    public Document Document { get; set; }
}