using WebApp.Models;

namespace WebApp.Interfaces
{
    public interface IBufferedFileUploadService
    {
        Task<bool> UploadFile(IFormFile file, dynamic UserName, ApplicationContext db);
    }
}
