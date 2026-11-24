namespace TaxFiler.Model.Dto;

/// <summary>
/// Represents a single link between a transaction and a document in the
/// many-to-many relationship, including its ordering metadata.
/// </summary>
public class TransactionDocumentDto
{
    public int Id { get; init; }
    public int TransactionId { get; init; }
    public int DocumentId { get; init; }
    public DocumentDto? Document { get; init; }
    public int Sequence { get; init; }
    public DateTime AssignedAt { get; init; }
    public string? AssignedBy { get; init; }
    public string? Notes { get; init; }
}
