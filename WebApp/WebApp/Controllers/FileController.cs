using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Runtime.ConstrainedExecution;
using WebApp.Interfaces;
using WebApp.Models;
using OfficeOpenXml;
using System.Diagnostics;
using System.Drawing;

namespace WebApp.Controllers
{
    [Authorize]
    public class FileController : Controller
    {
        readonly IBufferedFileUploadService _bufferedFileUploadService;
        /*private static int i = 0;*/


       /* private bool IsUpload = false;
        private bool IsProcessed = false;*/

        public FileController(IBufferedFileUploadService bufferedFileUploadService)
        {
            _bufferedFileUploadService = bufferedFileUploadService;
        }

        public IActionResult Index() => View();

        /*public IActionResult AllFiles()
        {
            ViewBag.Name = User.Identity.Name;
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            
            return View(LookIn($"C:\\Users\\Comp_Zona\\Desktop\\WebDev\\WebApp\\WebApp\\UploadedFiles\\{User.Identity.Name}"));
        }



        private List<string> LookIn(string path)
        {
            List<string> files = new List<string>();
            List<string> dirs = new List<string>();

            try
            {
                files.AddRange(Directory.GetFiles(path));
                dirs.AddRange(Directory.GetDirectories(path));
            }
            catch (UnauthorizedAccessException e)
            {
                //Console.WriteLine(e.Message);
            }

            foreach (string dir in dirs)
            {
                files.AddRange(LookIn(dir));
            }
            return files;
        }*/







        /* public IActionResult Upload() => View();*/

        [RequestFormLimits(MultipartBodyLengthLimit = 1048576000)] // 1000 MB
        [RequestSizeLimit(1048576000)] // 1000 MB
        [HttpPost]
        public async Task<ActionResult> Upload(IFormFile file)
        {
            try
            {
                if (await _bufferedFileUploadService.UploadFile(file, User.Identity.Name))
                {
                    ViewBag.Message = "File Upload Successful";
                }
                   
                
                else
                {
                    ViewBag.Message = "File Upload Failed";
                    ViewBag.IsUpload = true;
                }
                    
            }
            catch (Exception ex)
            {
                ViewBag.Message = "File Upload Failed";
            }
           
            return RedirectToAction("index", "file");
        }

        [HttpPost]
        public async Task<ActionResult> Process(IFormFile file)
        {
            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Download(IFormFile file)
        {
            return View();
        }



        [NonAction]
        public static void ConvertXlsxToTxt(string xlsxFilePath, string txtFilePath)
        {
            /*Console.WriteLine("Sleep...5s");
            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(1000);
                Console.WriteLine(i + 1);
            }
            Console.WriteLine("Start...");*/
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            FileInfo file = new FileInfo(xlsxFilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(txtFilePath));
            using (ExcelPackage package = new ExcelPackage(file))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                int rowCount = worksheet.Dimension.Rows;
                int colCount = worksheet.Dimension.Columns;
                using (StreamWriter writer = new StreamWriter(txtFilePath))
                {
                    string horizontalLine = new string('-', colCount * 26 + 17);
                    writer.WriteLine(horizontalLine);
                    for (int col = 1; col <= colCount; col++)
                        writer.Write($"| {worksheet.Cells[1, col].Text,-25}");
                    writer.WriteLine("|");
                    writer.WriteLine(horizontalLine);
                    for (int row = 2; row <= rowCount; row++)
                    {
                        for (int col = 1; col <= colCount; col++)
                            writer.Write($"| {worksheet.Cells[row, col].Text,-25}");
                        writer.WriteLine("|");
                    }

                    writer.WriteLine(horizontalLine);
                }
            }
            Console.WriteLine("Stop...");
        }


        [NonAction]
        private static void MeasureTime(Action action)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            action();

            stopwatch.Stop();
            long elapsedTimeMilliseconds = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Elapsed Time: {elapsedTimeMilliseconds} ms");
        }

        /*public async Task<ActionResult> Upload(IFormFile file)
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
        }*/



    }
}
