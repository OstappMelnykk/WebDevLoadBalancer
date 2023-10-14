using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Runtime.ConstrainedExecution;
using WebApp.Interfaces;
using WebApp.Models;
using OfficeOpenXml;
using System.Diagnostics;
using System.Drawing;
using LicenseContext = OfficeOpenXml.LicenseContext;

namespace WebApp.Controllers
{
    [Authorize]
    public class FileController : Controller
    {
        readonly IBufferedFileUploadService _bufferedFileUploadService;
        ApplicationContext db;

        public FileController(ApplicationContext context, IBufferedFileUploadService bufferedFileUploadService)
        {
            _bufferedFileUploadService = bufferedFileUploadService;
            db = context;
        }

        

        public IActionResult Index() => View(db.FilesToConvet.ToList());


        [HttpPost]
        public async Task<ActionResult> Process()
        {
            var files = db.FilesToConvet.ToList();


            foreach (var item in files)
            {
                string filePath = item.FullPathToFile;

                ConvertXlsxToTxt(filePath, item.FileName.Split(".")[0], db);


                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        System.IO.File.Delete(filePath);
                    }
                    catch (Exception e)
                    {
                        return Content($"Сталася помилка при видаленні файлу: {e.Message}");
                    }
                }
            }

            db.DeleteFromFilesToConvet();


            return RedirectToAction("Index", "file");
        }





        [NonAction]
        private void ConvertXlsxToTxt(string xlsxFilePath, string newFileName, ApplicationContext db)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            FileInfo file = new FileInfo(xlsxFilePath);
            string parentDirectory = Directory.GetParent(xlsxFilePath).FullName;
            string txtFilePath = Path.Combine(parentDirectory, "ConvertedFiles");
            
            Directory.CreateDirectory(txtFilePath);

            using (ExcelPackage package = new ExcelPackage(file))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                int rowCount = worksheet.Dimension.Rows;
                int colCount = worksheet.Dimension.Columns;

                string horizontalLine = new string('-', colCount * 26 + 17);

                using (StreamWriter writer = new StreamWriter(Path.Combine(txtFilePath, $"{newFileName}.txt")))
                {
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



            FileAlreadyConverted fileModel = new FileAlreadyConverted()
            {
                FileName = $"{newFileName}.txt",
                PathToFolder = txtFilePath,
                FullPathToFile = txtFilePath + "\\" + $"{newFileName}.txt",
            };
            db.FilesAlreadyConverted.Add(fileModel);
            db.SaveChanges();
        }


        [RequestFormLimits(MultipartBodyLengthLimit = 1048576000)] // 1000 MB
        [RequestSizeLimit(1048576000)] // 1000 MB
        [HttpPost]
        public async Task<ActionResult> Upload(IFormFile file)
        {
            try
            {
                if (await _bufferedFileUploadService.UploadFile(file, User.Identity.Name, db))
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
           
            return RedirectToAction("Index", "file");
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
    }
}
