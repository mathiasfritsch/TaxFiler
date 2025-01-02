using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxFiler.DB.Model;

public class Transaction
{
    [Key]
    public int Id { get; set; }
    public decimal NetAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxRate { get; set; }
    [MaxLength(200)]
    public String Counterparty { get; set; }
    [MaxLength(200)]
    public String TransactionReference { get; set; }
    public DateTime TransactionDateTime { get; set; }
    [MaxLength(200)]
    public string TransactionNote { get; set; }
    public bool IsOutgoing { get; set; }
    public bool IsIncomeTaxRelevant { get; set; }
    public bool IsSalesTaxRelevant { get; set; }
    public int TaxMonth { get; set; }
    public int TaxYear { get; set; }
    [ForeignKey(nameof(Document))]
    public int? DocumentId { get; set; }
    public Document? Document { get; set; }
    public string SenderReceiver { get; set; }
}