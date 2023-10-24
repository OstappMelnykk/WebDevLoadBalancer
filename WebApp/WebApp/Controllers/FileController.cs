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
using Aspose.Cells.Charts;
using System.ComponentModel.DataAnnotations;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;


namespace WebApp.Controllers
{
    [Authorize]
    public class FileController : Controller
    {
        readonly IBufferedFileUploadService _bufferedFileUploadService;
        private readonly IAzureBlobStorageService _azureBlobStorageService;
        ApplicationContext db;

        public FileController(ApplicationContext context, IBufferedFileUploadService bufferedFileUploadService, IAzureBlobStorageService azureBlobStorageService)
        {
            _bufferedFileUploadService = bufferedFileUploadService;
            db = context;
            _azureBlobStorageService = azureBlobStorageService;
        }

        public IActionResult Index()
        {
            ViewBag.DBFilesToConvert = db.FilesToConvert.Where(f => f.UserName == User.Identity.Name).ToList();
            ViewBag.DBConvertedFiles = db.ConvertedFiles.Where(f => f.UserName == User.Identity.Name).ToList();
            return View();
        }


       

        [RequestFormLimits(MultipartBodyLengthLimit = 209715200)] // 200 MB
        [RequestSizeLimit(209715200)] // 200 MB
        [HttpPost]
        public async Task<ActionResult> Upload(IFormFile file)
        {
            try{
                if (await _bufferedFileUploadService.UploadFile(file, User.Identity.Name, db)) ViewBag.Message = "File Upload Successful";
                else
                {
                    ViewBag.Message = "File Upload Failed";
                    ViewBag.IsUpload = true;
                }
            }
            catch (Exception ex) { ViewBag.Message = "File Upload Failed"; }
            return RedirectToAction("Index", "file");
        }



        [HttpPost]
        public async Task<ActionResult> Process()
        {
            var files = db.FilesToConvert.Where(f => f.UserName == User.Identity.Name).ToList();

            foreach (var item in files)
            {
                string filePath = item.FullPath;
                string Title = item.Title.Split(".")[0];

                ConvertXlsxToTxt(filePath, Title, db);

                db.DeleteFilesToConvertByUserName(User.Identity.Name.ToString());

                if (System.IO.File.Exists(filePath))
                {
                    try{System.IO.File.Delete(filePath);}
                    catch (Exception e){}
                }
            }
            return RedirectToAction("Index", "file");
        }




       
        //Отримання даних із файлу XLSX:
        private ExcelPackage GetExcelPackage(string xlsxFilePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            FileInfo file = new FileInfo(xlsxFilePath);
            return new ExcelPackage(file);
        }

        //Обчислення ширини стовпців:
        private int[] CalculateColumnWidths(ExcelWorksheet worksheet)
        {
            int rowCount = worksheet.Dimension.Rows;
            int colCount = worksheet.Dimension.Columns;
            int[] columnWidths = new int[colCount];

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

            return columnWidths;
        }

        //Форматування та запис в файл TXT:
        private void WriteToTxtFile(ExcelWorksheet worksheet, int[] columnWidths, string txtFilePath, string newFileName)
        {
            int rowCount = worksheet.Dimension.Rows;
            int colCount = worksheet.Dimension.Columns;
            int maxLineWidth = columnWidths.Sum() + 4 * colCount;
            string horizontalLine = new string('-', maxLineWidth);


          
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
        //Збереження файлу в базу даних:
        private void SaveFileToDatabase(ApplicationContext db, string newFileName, string txtFilePath)
        {
            User user = db.Users.SingleOrDefault(u => u.UserName == User.Identity.Name);

            AddFileTo_ConvertedFiles_Db(
                $"{newFileName}.txt",
                txtFilePath,
                Path.Combine(txtFilePath, $"{newFileName}.txt"),
                User.Identity.Name.ToString(),
                user.Id,
                user,
                db
            );
        }

        [NonAction]
        private void ConvertXlsxToTxt(string xlsxFilePath, string newFileName, ApplicationContext db)
        {
            ExcelPackage package = GetExcelPackage(xlsxFilePath);
            ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
            int[] columnWidths = CalculateColumnWidths(worksheet);

            string parentDirectory = Directory.GetParent(xlsxFilePath).FullName;
            string txtFilePath = Path.Combine(parentDirectory, "ConvertedFiles");
            Directory.CreateDirectory(txtFilePath);

            WriteToTxtFile(worksheet, columnWidths, txtFilePath, newFileName);
            SaveFileToDatabase(db, newFileName, txtFilePath);

            package.Dispose();
        }
        /*
                [NonAction]
                private void ConvertXlsxToTxt(string xlsxFilePath, string newFileName, ApplicationContext db)
                {
                    ExcelPackage package = GetExcelPackage(xlsxFilePath);
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                    int[] columnWidths = CalculateColumnWidths(worksheet);

                    string parentDirectory = Directory.GetParent(xlsxFilePath).FullName;
                    string txtFilePath = Path.Combine(parentDirectory, "ConvertedFiles");
                    Directory.CreateDirectory(txtFilePath);

                    WriteToTxtFile(worksheet, columnWidths, txtFilePath, newFileName);


                    string connectionString = "DefaultEndpointsProtocol=https;AccountName=webdevblobstorage111;AccountKey=34UT1RhvVbZySCEFwnvUjB6QWytyioSSE2dM3X5XPPS/riC1AhzmmckT+hlCRLn4JRCwAynDQY+4+AStSDlBmg==;EndpointSuffix=core.windows.net";
                    BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

                    // Отримайте посилання на контейнер, в якому ви хочете зберегти TXT файл.
                    BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("webdev");
                    containerClient.CreateIfNotExists();

                    // Створіть BlobClient для TXT файлу та завантажте його.
                    BlobClient blobClient = containerClient.GetBlobClient($"{newFileName}.txt");
                    using (FileStream stream = System.IO.File.OpenRead(Path.Combine(txtFilePath, $"{newFileName}.txt")))
                    {
                        blobClient.Upload(stream, true);
                    }




                    SaveFileToDatabase(db, newFileName, txtFilePath);

                    package.Dispose();
                }*/





















        /*[RequestFormLimits(MultipartBodyLengthLimit = 209715200)] // 200 MB
        [RequestSizeLimit(209715200)] // 200 MB
        [HttpPost]
        public async Task<ActionResult> Upload(IFormFile file)
        {
            try
            {
                string uploadedFileUri = await _azureBlobStorageService.UploadFileAsync(file, User.Identity.Name, db);
                if (!string.IsNullOrEmpty(uploadedFileUri))
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
        }     */



        [NonAction]
        private void AddFileTo_ConvertedFiles_Db(
            string _Title,
            string _Path,
            string _FullPath,
            string _UserName,
            string _UserId,
            User _User,
            ApplicationContext db)
        {
            ConvertedFile fileModel = new ConvertedFile()
            {
                Title = _Title,
                Path = _Path,
                FullPath = _FullPath,
                UserName = _UserName,
                UserId = _UserId,
                User = _User
            };

            db.ConvertedFiles.Add(fileModel);
            db.SaveChanges();
        }



        [NonAction]
        private void AddFileTo_FilesToConvert_Db(
            string _Title,
            string _Path,
            string _FullPath,
            string _UserName,
            string _UserId,
            User _User,
            ApplicationContext db)
        {
            FileToConvert fileModel = new FileToConvert()
            {
                Title = _Title,
                Path = _Path,
                FullPath = _FullPath,
                UserName = _UserName,
                UserId = _UserId,
                User = _User
            };

            db.FilesToConvert.Add(fileModel);
            db.SaveChanges();
        }





        [HttpPost]
        public async Task<ActionResult> Download(string fullpath, string title)
        {
            string filePath = fullpath;
            string fileName = title;

            return File(System.IO.File.OpenRead(filePath), "text/plain", fileName);
        }

        [HttpPost]
        public async Task<ActionResult> DeleteConverted(string fullpath)
        {
            db.DeleteConvertedFilesByUserNameAndFullPath(User.Identity.Name.ToString(), fullpath);
            if (System.IO.File.Exists(fullpath))
            {
                try { System.IO.File.Delete(fullpath); }
                catch (Exception e) { }
            }

            return RedirectToAction("Index", "file");
        }

        [HttpPost]
        public async Task<ActionResult> DeleteUploaded(string fullpath)
        {
            db.DeleteFilesToConvertByUserNameAndFullPath(User.Identity.Name.ToString(), fullpath);
            if (System.IO.File.Exists(fullpath))
            {
                try { System.IO.File.Delete(fullpath); }
                catch (Exception e) { }
            }
            return RedirectToAction("Index", "file");
        }

        [HttpPost]
        public async Task<ActionResult> Delete_All_Converted()
        {
            var DBConvertedFiles = db.ConvertedFiles.Where(f => f.UserName == User.Identity.Name).ToList();

            foreach (var item in DBConvertedFiles)
            {
                string fullpath = item.FullPath;
                if (System.IO.File.Exists(fullpath))
                {
                    try { System.IO.File.Delete(fullpath); }
                    catch (Exception e) { }
                }
            }

            db.DeleteConvertedFilesByUserName(User.Identity.Name.ToString());

            return RedirectToAction("Index", "file");
        }

        [HttpPost]
        public async Task<ActionResult> Delete_All_Uploaded()
        {
            var DBFilesToConvert = db.FilesToConvert.Where(f => f.UserName == User.Identity.Name).ToList();
            foreach (var item in DBFilesToConvert)
            {
                string fullpath = item.FullPath;
                if (System.IO.File.Exists(fullpath))
                {
                    try { System.IO.File.Delete(fullpath); }
                    catch (Exception e) { }
                }
            }
            db.DeleteFilesToConvertByUserName(User.Identity.Name.ToString());

            return RedirectToAction("Index", "file");
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

    }
}