using Azure.Storage.Blobs;
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

        public async Task<string> UploadFileAsync(IFormFile file, string userName, ApplicationContext context)
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
                    FileToConvert fileModel = new FileToConvert()
                    {
                        Title = fileName,
                        Path = folderPath,
                        FullPath = folderPath + fileName,
                        UserName = userName,
                        UserId = user.Id.ToString(),
                        User = user
                    };
                    context.FilesToConvert.AddRange(fileModel);
                    context.SaveChanges();
                }

                return blobClient.Uri.ToString();
            }

            return null;
        }
    }
}
