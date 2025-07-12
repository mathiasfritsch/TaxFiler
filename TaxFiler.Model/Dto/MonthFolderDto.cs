namespace TaxFiler.Model.Dto;

public class MonthFolderDto
{
    public required string Month { get; init; }
    public required string FolderId { get; init; }
    public int MonthNumber => int.TryParse(Month, out var month) ? month : 0;
}
