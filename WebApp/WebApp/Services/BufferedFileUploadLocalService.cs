using WebApp.Interfaces;
using WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebApp.Models;

namespace WebApp.Services
{
    public class BufferedFileUploadLocalService : IBufferedFileUploadService
    {
        private static int i = 1;
        public async Task<bool> UploadFile(IFormFile file, dynamic UserName)
        {
            string path = "";

            try{

                if (file.Length > 0){

                   path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, $"UploadedFiles/{UserName}"));

                   if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                   using (var fileStream = new FileStream(Path.Combine(path, $"{i}--{UserName}--{file.FileName}"), FileMode.Create)){
                       await file.CopyToAsync(fileStream);
                       i++;
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
