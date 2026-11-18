using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxFiler.DB.Model;

/// <summary>
/// Junction entity implementing a many-to-many relationship between
/// <see cref="Transaction"/> and <see cref="Document"/>. A single document may
/// support multiple transactions (for example a split invoice) and a single
/// transaction may be supported by multiple documents (for example a combined
/// payment covering several invoices).
/// </summary>
public class TransactionDocument
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Transaction))]
    public int TransactionId { get; set; }
    public Transaction Transaction { get; set; } = null!;

    [ForeignKey(nameof(Document))]
    public int DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    /// <summary>
    /// Deterministic order of this document within its transaction's collection.
    /// Sequence numbers are unique per transaction.
    /// </summary>
    public int Sequence { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? AssignedBy { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
