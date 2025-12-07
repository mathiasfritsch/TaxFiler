using Google.Apis.Drive.v3;
using Google.Apis.Services;
using System.Text;
using Microsoft.Extensions.Options;
using TaxFiler.Model;
using TaxFiler.Model.Dto;

namespace TaxFiler.Service
{
    public class GoogleDriveService(IOptions<GoogleDriveSettings> settings) : IGoogleDriveService
    {
        public async Task<List<FileData>> GetFilesAsync(DateOnly date)
        {
            var service =  CreateDriveService();

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
            var request = service.Files.List();
            
            request.Q = string.IsNullOrEmpty(parentId) ? $"mimeType='application/vnd.google-apps.folder' and name='{folderName}' and trashed=false" :

                $"mimeType='application/vnd.google-apps.folder' and '{parentId}' in parents and name='{folderName}' and trashed=false";
            
            request.Fields = "files(id, name)";
            request.PageSize = 1;
            
            var result = await request.ExecuteAsync();
            return result.Files.Single().Id;
        }
        
        public async Task<byte[]> DownloadFileAsync(string fileId)
        {
            var service =  CreateDriveService();

            var stream = new MemoryStream();
            await service.Files.Get(fileId).DownloadAsync(stream);

            return stream.ToArray();
        }

        public async Task<GoogleDriveFolderStructureDto> GetFolderStructureAsync()
        {
            var service = CreateDriveService();

            var taxFilerFolderId = await FindFolderIdAsync(service, "TaxFiler", "");

            var yearFoldersRequest = service.Files.List();
            yearFoldersRequest.Q = $"mimeType='application/vnd.google-apps.folder' and '{taxFilerFolderId}' in parents and trashed=false";
            yearFoldersRequest.Fields = "files(id, name)";
            yearFoldersRequest.PageSize = 100;

            var yearFoldersResult = await yearFoldersRequest.ExecuteAsync();

            var yearFolders = yearFoldersResult.Files
                .Where(f => IsYearFolder(f.Name))
                .OrderByDescending(f => f.Name)
                .ToList();

            var yearFolderDtos = new List<YearFolderDto>();

            foreach (var yearFolder in yearFolders)
            {
                var monthFolders = await GetMonthFoldersAsync(service, yearFolder.Id);

                yearFolderDtos.Add(new YearFolderDto
                {
                    Year = yearFolder.Name,
                    FolderId = yearFolder.Id,
                    MonthFolders = monthFolders
                });
            }

            return new GoogleDriveFolderStructureDto
            {
                YearFolders = yearFolderDtos
            };
        }

        private DriveService CreateDriveService()
        {
            string[] scopes = [DriveService.Scope.DriveReadonly];

            string secretEnc = settings.Value.GoogleApplicationCredentials;
            byte[] decodedBytes = Convert.FromBase64String(secretEnc);
            string decodedString = Encoding.UTF8.GetString(decodedBytes);

            var credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromJson(decodedString).CreateScoped(scopes);

            return new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "TaxFiler",
            });
        }

        private async Task<List<MonthFolderDto>> GetMonthFoldersAsync(DriveService service, string yearFolderId)
        {
            var monthFoldersRequest = service.Files.List();
            monthFoldersRequest.Q = $"mimeType='application/vnd.google-apps.folder' and '{yearFolderId}' in parents and trashed=false";
            monthFoldersRequest.Fields = "files(id, name)";
            monthFoldersRequest.PageSize = 20;

            var monthFolders = await monthFoldersRequest.ExecuteAsync();

            return monthFolders.Files
                .Where(f => IsMonthFolder(f.Name))
                .OrderBy(f => int.TryParse(f.Name, out var month) ? month : 0)
                .Select(f => new MonthFolderDto
                {
                    Month = f.Name,
                    FolderId = f.Id
                })
                .ToList();
        }

        private static bool IsYearFolder(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName) || folderName.Length != 4)
                return false;

            return int.TryParse(folderName, out var year) && year >= 2000 && year <= 2100;
        }

        private static bool IsMonthFolder(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName) || folderName.Length != 2)
                return false;

            return int.TryParse(folderName, out var month) && month >= 1 && month <= 12;
        }
    }


}