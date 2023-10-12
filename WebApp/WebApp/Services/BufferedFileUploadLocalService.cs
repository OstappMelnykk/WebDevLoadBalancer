using WebApp.Interfaces;

namespace WebApp.Services
{
    public class BufferedFileUploadLocalService : IBufferedFileUploadService
    {
        private string variable = "VAR";

        public async Task<bool> UploadFile(IFormFile file)
        {
            string path = "";

            try{

                if (file.Length > 0){

                    path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "UploadedFiles"));

                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                    using (var fileStream = new FileStream(Path.Combine(path, $"{file.FileName.Split(".")[0]}{variable}.{file.FileName.Split(".")[1]}"), FileMode.Create)){
                        await file.CopyToAsync(fileStream);
                    }

                    return true;
                }

                else return false;
            }
            catch (Exception ex)
            {
                throw new Exception("File Copy Failed", ex);
            }
        }
    }
}
