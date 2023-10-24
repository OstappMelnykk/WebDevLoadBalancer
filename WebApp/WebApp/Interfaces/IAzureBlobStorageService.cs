using WebApp.Models;

namespace WebApp.Interfaces
{
    public interface IAzureBlobStorageService
    {
        Task<string> UploadFileAsync(IFormFile file, string userName, ApplicationContext context);
    }
}
