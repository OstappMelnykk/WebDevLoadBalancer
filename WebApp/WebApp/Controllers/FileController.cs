using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.ConstrainedExecution;
using WebApp.Interfaces;
using WebApp.Models;

namespace WebApp.Controllers
{
    [Authorize]
    public class FileController : Controller
    {
        readonly IBufferedFileUploadService _bufferedFileUploadService;
        private static int i = 0;

       /* var NAme = User.Identity.Name;*/


        public FileController(IBufferedFileUploadService bufferedFileUploadService)
        {
            _bufferedFileUploadService = bufferedFileUploadService;
        }

        public IActionResult Upload() => View();


        [RequestFormLimits(MultipartBodyLengthLimit = 1048576000)] // 1000 MB
        [RequestSizeLimit(1048576000)] // 1000 MB
        [HttpPost]
        public async Task<ActionResult> Upload(IFormFile file)
        {
            try
            {
                if (await _bufferedFileUploadService.UploadFile(file, User.Identity.Name))
                    ViewBag.Message = "File Upload Successful";
                else
                    ViewBag.Message = "File Upload Failed";
            }
            catch (Exception ex)
            {
                ViewBag.Message = "File Upload Failed";
            }
            return View();
        }
    }
}
