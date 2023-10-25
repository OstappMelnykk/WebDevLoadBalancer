using OfficeOpenXml;
using WebApp.Models;

namespace WebApp.Interfaces
{
    public interface IAzureBlobStorageService
    {
        Task<string> UploadFileAsync_TO_FilesToConvert(IFormFile file, string userName, ApplicationContext context);
        Task<string> UploadFileAsync_TO_ConvertedFiles(string textContent, string userName, string title, ApplicationContext context);
        Task<ExcelPackage> GetExcelPackageFromAzureBlob(string path);
        Task DeleteBlobAsync(string path);
    }
}
