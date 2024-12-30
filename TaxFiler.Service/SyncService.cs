using TaxFiler.DB;
using TaxFiler.DB.Model;

namespace TaxFiler.Service;

public class SyncService(TaxFilerContext context, IGoogleDriveService googleDriveService)
    : ISyncService
{
    public async Task SyncFilesAsync(DateOnly date)
    {
        var files = await googleDriveService.GetFilesAsync(date);
        
        foreach(var document in context.Documents)
        {
            if(files.All(f => f.Id != document.ExternalRef))
            {
                if( !document.Orphaned )
                    document.Orphaned = true;
            }
            else
            {
                if( document.Orphaned )
                    document.Orphaned = false;
            }
        }
        
        foreach (var file in 
                 files.Where( f =>!context.Documents.Any(d => d.ExternalRef == f.Id)))
        {
            var document = new Document
            {
                Name = file.Name,
                ExternalRef = file.Id,
                Orphaned = false,
                TaxYear = date.Year,
                TaxMonth = date.Month
            };
            await context.Documents.AddAsync(document);
        }
        
        await context.SaveChangesAsync();
    }
}