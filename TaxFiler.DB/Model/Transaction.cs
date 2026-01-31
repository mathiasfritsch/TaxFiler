using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxFiler.DB.Model;

public class Transaction
{
    [Key]
    public int Id { get; set; }
    
    [ForeignKey(nameof(Account))]
    public int AccountId { get; set; }
    public Account Account { get; set; }
    public decimal? NetAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? TaxRate { get; set; }
    [MaxLength(200)]
    public String Counterparty { get; set; }

    [MaxLength(200)] public String TransactionReference { get; set; } = "";
    public DateTime TransactionDateTime { get; set; }
    [MaxLength(200)]
    public string TransactionNote { get; set; }
    public bool IsOutgoing { get; set; }
    public bool? IsIncomeTaxRelevant { get; set; }
    public bool? IsSalesTaxRelevant { get; set; }
    public int? TaxMonth { get; set; } = 0;
    public int? TaxYear { get; set; } = 0;
    
    public string SenderReceiver { get; set; }
    
    /// <summary>
    /// Collection of document attachments for this transaction.
    /// Supports multiple documents being attached to a single transaction.
    /// </summary>
    public ICollection<DocumentAttachment> DocumentAttachments { get; set; } = new List<DocumentAttachment>();
}