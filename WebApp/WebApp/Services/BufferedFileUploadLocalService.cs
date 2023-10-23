using WebApp.Interfaces;
using WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebApp.Models;

namespace WebApp.Services
{

    //IFormFile file, dynamic UserName, ApplicationContext db, string FolderName
    public class BufferedFileUploadLocalService : IBufferedFileUploadService
    {
        private static int i = 1;
        public async Task<bool> UploadFile(IFormFile file, object UserName, ApplicationContext context)
        {
            string path = "";

            try
            {

                if (file.Length > 0)
                {

                    path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, $"UploadedFiles/{UserName.ToString()}"));

                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                    using (var fileStream = new FileStream(Path.Combine(path, $"{i}--{file.FileName}"), FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);

                        User user = context.Users.SingleOrDefault(u => u.UserName == UserName.ToString());
                        FileToConvert fileModel = new FileToConvert()
                        {
                            Title = $"{i}--{file.FileName}",
                            Path = path,
                            FullPath = path + "\\" + $"{i}--{file.FileName}",
                            UserName = UserName.ToString(),
                            UserId = user.Id.ToString(),
                            User = user // Set the User navigation property
                        };
                        context.FilesToConvert.AddRange(fileModel);
                        context.SaveChanges();


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
