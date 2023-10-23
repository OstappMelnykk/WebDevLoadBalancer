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

        

        /*public IActionResult Index() => View(db.FilesToConvet.ToList());*/


        public IActionResult Index()
        {
            ViewBag.DBFilesToConvet = db.FilesToConvert.ToList();
            ViewBag.DBFilesAlreadyConverted = db.ConvertedFiles.ToList();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Process()
        {
            var files = db.FilesToConvert.ToList();




            long elapsedTimeMilliseconds = MeasureTime(() =>
            {

                foreach (var item in files)
                {
                    string filePath = item.FullPath;

                    ConvertXlsxToTxt(filePath, item.Title.Split(".")[0], db);


                    if (System.IO.File.Exists(filePath))
                    {
                        try
                        {
                            System.IO.File.Delete(filePath);
                        }
                        catch (Exception e)
                        {
                            /*return Content($"Сталася помилка при видаленні файлу: {e.Message}");*/
                        }
                    }
                }

                db.DeleteFromFilesToConvert();



            });






            return Content($"Elapsed Time: {elapsedTimeMilliseconds} ms");
            //return RedirectToAction("Index", "file");
        }



        [NonAction]
        private long MeasureTime(Action action)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            action();

            stopwatch.Stop();
            long elapsedTimeMilliseconds = stopwatch.ElapsedMilliseconds;
            return elapsedTimeMilliseconds;
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




                int[] columnWidths = new int[colCount]; // Додайте це тут для обчислення ширини стовбців

                for (int col = 1; col <= colCount; col++)
                {
                    int maxColumnWidth = 0;
                    for (int row = 1; row <= rowCount; row++)
                    {
                        int cellWidth = worksheet.Cells[row, col].Text.Length;
                        if (cellWidth > maxColumnWidth)
                        {
                            maxColumnWidth = cellWidth;
                        }
                    }
                    columnWidths[col - 1] = maxColumnWidth;
                }




                int maxLineWidth = 0;
                for (int col = 0; col < colCount; col++)
                {
                    maxLineWidth += columnWidths[col] + 4; // +4 для роздільних ліній і відступів
                }

                string horizontalLine = new string('-', maxLineWidth);

                /*string horizontalLine = new string('-', colCount * 26 + 17);*/

                using (StreamWriter writer = new StreamWriter(Path.Combine(txtFilePath, $"{newFileName}.txt")))
                {
                    writer.WriteLine(horizontalLine);

                    for (int col = 1; col <= colCount; col++)
                    {
                        int a = -(columnWidths[col - 1] + 2);
                        writer.Write(string.Format("| {0," + a + "}", worksheet.Cells[1, col].Text));
                    }
                    writer.WriteLine("|");
                    writer.WriteLine(horizontalLine);

                    for (int row = 2; row <= rowCount; row++)
                    {
                        for (int col = 1; col <= colCount; col++)
                        {
                            int a = -(columnWidths[col - 1] + 2);
                            writer.Write(string.Format("| {0," + a + "}", worksheet.Cells[row, col].Text));

                        }
                        writer.WriteLine("|");
                    }

                    writer.WriteLine(horizontalLine);
                }


            }



            ConvertedFile fileModel = new ConvertedFile()
            {
                Title = $"{newFileName}.txt",
                Path = txtFilePath,
                FullPath = txtFilePath + "\\" + $"{newFileName}.txt",
            };
            db.ConvertedFiles.Add(fileModel);
            db.SaveChanges();
        }












        /*[NonAction]
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
                        //writer.Write($"| {worksheet.Cells[1, col].Text,-25}");

                    writer.WriteLine("|");
                    writer.WriteLine(horizontalLine);

                    for (int row = 2; row <= rowCount; row++)
                    {
                        for (int col = 1; col <= colCount; col++)
                            writer.Write($"| {worksheet.Cells[row, col].Text,-25}");
                            //writer.Write($"| {worksheet.Cells[row, col].Text,-25}");

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
        }*/


        /*[RequestFormLimits(MultipartBodyLengthLimit = 1048576000)] // 1000 MB
        [RequestSizeLimit(1048576000)] // 1000 MB*/


        [RequestFormLimits(MultipartBodyLengthLimit = 209715200)] // 200 MB
        [RequestSizeLimit(209715200)] // 200 MB
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


  
        
    }
}



/*select* from public."AspNetUsers";
select* from public."FilesAlreadyConverted";
select* from public."FilesToConvet";


delete from public."AspNetUsers";
delete from public."FilesAlreadyConverted";
delete from public."FilesToConvet";*/
