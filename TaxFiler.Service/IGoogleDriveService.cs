using TaxFiler.Model;
using TaxFiler.Model.Dto;

namespace TaxFiler.Service;

public interface IGoogleDriveService
{
    public Task<List<FileData>> GetFilesAsync(DateOnly date);

    public Task<Byte[]> DownloadFileAsync(string fileId);

    /// <summary>
    /// Gets the folder structure from Google Drive root, including year folders and their optional month subfolders
    /// </summary>
    /// <returns>A DTO containing the hierarchical folder structure</returns>
    public Task<GoogleDriveFolderStructureDto> GetFolderStructureAsync();
}