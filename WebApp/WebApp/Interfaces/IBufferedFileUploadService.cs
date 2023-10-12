namespace WebApp.Interfaces
{
    public interface IBufferedFileUploadService
    {
        Task<bool> UploadFile(IFormFile file, dynamic UserName);
    }
}
