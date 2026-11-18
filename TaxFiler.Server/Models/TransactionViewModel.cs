namespace TaxFiler.Models;

public class TransactionViewModel
{
    public int Id { get; init; }
    public decimal GrossAmount { get; init; }
    public string? Counterparty { get; init; }
    public string? SenderReceiver { get; init; }
    public string? TransactionNote { get; init; }
    public string? TransactionReference { get; init; }
    public DateTime TransactionDateTime { get; init; }
    public decimal NetAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TaxRate { get; init; }
    public bool IsOutgoing { get; init; }
    public bool IsIncomeTaxRelevant { get; init; }
    public bool IsSalesTaxRelevant { get; init; }
    public int AccountId { get; init; }
    public required string AccountName { get; init; }
    public bool IsTaxMismatch { get; init; }

    /// <summary>IDs of all documents linked to this transaction, ordered by sequence.</summary>
    public IReadOnlyList<int> DocumentIds { get; init; } = new List<int>();

    /// <summary>Names of all documents linked to this transaction, ordered by sequence.</summary>
    public IReadOnlyList<string> DocumentNames { get; init; } = new List<string>();

    /// <summary>ID of the primary (first) document. Backward-compatible convenience field.</summary>
    public int? DocumentId { get; init; }

    /// <summary>Name of the primary (first) document. Backward-compatible convenience field.</summary>
    public string? DocumentName { get; init; }
}
