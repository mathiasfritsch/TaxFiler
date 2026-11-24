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
    public bool IsTaxMismatchConfirmed { get; set; } = false;

    public string SenderReceiver { get; set; }

    /// <summary>
    /// Documents associated with this transaction. This is a many-to-many
    /// relationship: a transaction may be supported by several documents and a
    /// document may support several transactions.
    /// </summary>
    public ICollection<TransactionDocument> TransactionDocuments { get; set; } =
        new List<TransactionDocument>();
}
