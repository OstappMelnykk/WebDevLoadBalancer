using Aspose.Cells.Charts;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using OfficeOpenXml;
using System.Text;
using WebApp.Interfaces;
using WebApp.Models;

namespace WebApp.Services
{
    public class AzureBlobStorageService : IAzureBlobStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public AzureBlobStorageService(IConfiguration configuration)
        {
            string connectionString = configuration.GetSection("AzureBlobStorage")["ConnectionString"];
            string containerName = configuration.GetSection("AzureBlobStorage")["ContainerName"];

            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        }


        public async Task DeleteBlobAsync(string path)
        {
            try{await _containerClient.GetBlobClient(path).DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots);}
            catch (RequestFailedException ex){}
        }

        public async Task<ExcelPackage> GetExcelPackageFromAzureBlob(string path)
        {
            BlobClient blobClient = _containerClient.GetBlobClient(path);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                blobClient.DownloadTo(memoryStream);
                memoryStream.Position = 0;

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                return new ExcelPackage(memoryStream);
            }
        }

        private void AddFileTo_ConvertedFiles_Db(
            string _Title,
            string _Path,
            string _FullPath,
            string _UserName,
            string _UserId,
            User _User,
            ApplicationContext db)
        {
            ConvertedFile fileModel = new ConvertedFile()
            {
                Title = _Title,
                Path = _Path,
                FullPath = _FullPath,
                UserName = _UserName,
                UserId = _UserId,
                User = _User
            };

            db.ConvertedFiles.Add(fileModel);
            db.SaveChanges();
        }
   
        private void AddFileTo_FilesToConvert_Db(
            string _Title,
            string _Path,
            string _FullPath,
            string _UserName,
            string _UserId,
            User _User,
            ApplicationContext db)
        {
            FileToConvert fileModel = new FileToConvert()
            {
                Title = _Title,
                Path = _Path,
                FullPath = _FullPath,
                UserName = _UserName,
                UserId = _UserId,
                User = _User
            };

            db.FilesToConvert.Add(fileModel);
            db.SaveChanges();
        }

        public async Task<string> UploadFileAsync_TO_ConvertedFiles(string textContent, string userName, string title, ApplicationContext context)
        {
            string folderPath = $"{userName}/ConvertedFiles/"; 
            string fileName = $"{title}.txt";
          
            BlobClient blobClient = _containerClient.GetBlobClient(folderPath + fileName);
            await blobClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(textContent), true));

            //User user = context.Users.SingleOrDefault(u => u.UserName == userName.ToString());
            //AddFileTo_ConvertedFiles_Db(fileName, folderPath, folderPath + fileName, userName, user.Id.ToString(), user, context);
            
            return blobClient.Uri.ToString();
        }

        public async Task<string> UploadFileAsync_TO_FilesToConvert(IFormFile file, string userName, ApplicationContext context)
        {
            if (file.Length > 0)
            {
                string folderPath = $"{userName}/FilesToConvert/"; 
                string fileName = $"{Guid.NewGuid()}_{file.FileName}";

                BlobClient blobClient = _containerClient.GetBlobClient(folderPath + fileName);

                using (var fileStream = file.OpenReadStream())
                {
                    await blobClient.UploadAsync(fileStream, true);
                    User user = context.Users.SingleOrDefault(u => u.UserName == userName.ToString());
                    AddFileTo_FilesToConvert_Db(fileName, folderPath, folderPath + fileName, userName, user.Id.ToString(), user, context);                  
                }
                return blobClient.Uri.ToString();
            }
            return null;
        }
    }
}

