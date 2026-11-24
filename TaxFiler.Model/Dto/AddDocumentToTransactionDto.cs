namespace TaxFiler.Model.Dto;

/// <summary>
/// Payload for linking a document to a transaction.
/// </summary>
public class AddDocumentToTransactionDto
{
    public int DocumentId { get; set; }

    /// <summary>
    /// Optional position within the transaction's document collection. When null,
    /// the document is appended to the end of the collection.
    /// </summary>
    public int? Sequence { get; set; }

    public string? Notes { get; set; }
}
