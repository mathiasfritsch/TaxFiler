namespace TaxFiler.Model.Dto;

public class GoogleDriveFolderStructureDto
{
    public required List<YearFolderDto> YearFolders { get; init; }

    public int TotalYearFolders => YearFolders.Count;

    public int TotalMonthFolders => YearFolders.Sum(y => y.MonthFolders.Count);
}