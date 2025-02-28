using TaxFiler.Model;

namespace TaxFiler.Service;

public interface IGoogleDriveService
{
    public Task<List<FileData>> GetFilesAsync(DateOnly date);

    public Task<Byte[]> DownloadFileAsync(string fileId);
}