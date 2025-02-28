namespace TaxFiler.Service;

public interface ISyncService
{
    public Task SyncFilesAsync(DateOnly date);
    
}