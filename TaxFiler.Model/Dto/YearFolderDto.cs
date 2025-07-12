namespace TaxFiler.Model.Dto;

public class YearFolderDto
{
    public required string Year { get; init; }
    public required string FolderId { get; init; }
    public required List<MonthFolderDto> MonthFolders { get; init; }
}
