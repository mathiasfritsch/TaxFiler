using Google.Apis.Drive.v3;
using Google.Apis.Services;
using System.Text;
using Microsoft.Extensions.Options;
using TaxFiler.Model;

namespace TaxFiler.Service
{
    public class GoogleDriveService(IOptions<GoogleDriveSettings> settings) : IGoogleDriveService
    {
        public async Task<List<FileData>> GetFilesAsync(DateOnly date)
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

            var yearFolder = await FindFolderIdAsync(service,date.Year.ToString(),"" );
            var monthFolder = await FindFolderIdAsync(service,date.Month.ToString("D2"),yearFolder );

            var filesRequest =  service.Files.List();
            filesRequest.Q = $"'{monthFolder}' in parents";
            filesRequest.Fields = "files(id, name)";
            var filesList = await filesRequest.ExecuteAsync();
            
            return filesList.Files
                    .Where(f => f.MimeType != "application/vnd.google-apps.folder")
                    .Select(f => new FileData { Name = f.Name, Id = f.Id })
                    .ToList();
        }
        
        private async Task<string> FindFolderIdAsync(DriveService service, string folderName, string parentId)
        {
            var filesRequest = await service.Files.List().ExecuteAsync();
            
            if(parentId == "")
            {
                return filesRequest.Files
                    .Where(f => f.MimeType == "application/vnd.google-apps.folder")
                    .Single(f => f.Name == folderName)
                    .Id;
            }
            
            var request = service.Files.List();
            request.Q = $"'{parentId}' in parents and name contains '{folderName}'";
            request.Fields = "nextPageToken, files(id, name, mimeType)";
            
            var result = await request.ExecuteAsync();
            return result.Files.Single().Id;
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