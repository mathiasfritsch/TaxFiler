using Google.Apis.Drive.v3;
using Google.Apis.Services;
using System.Text;
using Microsoft.Extensions.Options;
using TaxFiler.Model;

namespace TaxFiler.Service
{
    public class GoogleDriveService(IOptions<GoogleDriveSettings> settings) : IGoogleDriveService
    {
        public async Task<List<FileData>> GetFilesAsync()
        {
            string[] scopes = [DriveService.Scope.DriveReadonly];

            string secretEnc = settings.Value.GoogleApplicationCredentials;

            byte[] decodedBytes = Convert.FromBase64String(secretEnc);
            string decodedString = Encoding.UTF8.GetString(decodedBytes);

            var credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromJson(decodedString).CreateScoped(scopes);

            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "TaxFiler",
            });

            var filesRequest = await service.Files.List().ExecuteAsync();

            return filesRequest.Files
                    .Where(f => f.MimeType != "application/vnd.google-apps.folder")
                    .Select(f => new FileData { Name = f.Name, Id = f.Id })
                    .ToList();
        }
        
        public async Task<byte[]> DownloadFileAsync(string fileId)
        {
            string[] scopes = [DriveService.Scope.DriveReadonly];

            string secretEnc = settings.Value.GoogleApplicationCredentials;

            byte[] decodedBytes = Convert.FromBase64String(secretEnc);
            string decodedString = Encoding.UTF8.GetString(decodedBytes);

            var credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromJson(decodedString).CreateScoped(scopes);

            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "TaxFiler",
            });
            
            var stream = new MemoryStream();
            await service.Files.Get(fileId).DownloadAsync(stream);

            return stream.ToArray();
        }
    }

 
}