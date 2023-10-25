﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Diagnostics;
using System.Text;
using WebApp.Interfaces;
using WebApp.Models;

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
            try
            {
                string uploadedFileUri = await _azureBlobStorageService.UploadFileAsync_TO_FilesToConvert(file, User.Identity.Name, db);
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
        }

        /*[RequestFormLimits(MultipartBodyLengthLimit = 209715200)] // 200 MB
        [RequestSizeLimit(209715200)] // 200 MB
        [HttpPost]
        public async Task<ActionResult> Upload(IFormFile file)
        {
            try
            {
                if (await _bufferedFileUploadService.UploadFile(file, User.Identity.Name, db)) ViewBag.Message = "File Upload Successful";
                else
                {
                    ViewBag.Message = "File Upload Failed";
                    ViewBag.IsUpload = true;
                }
            }
            catch (Exception ex) { ViewBag.Message = "File Upload Failed"; }
            return RedirectToAction("Index", "file");
        }*/



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

                await _azureBlobStorageService.DeleteBlobAsync(filePath);
            }
            return RedirectToAction("Index", "file");
        }




       
        //Отримання даних із файлу XLSX:
        /*private ExcelPackage GetExcelPackage(string xlsxFilePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            FileInfo file = new FileInfo(xlsxFilePath);
            return new ExcelPackage(file);
        }*/



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


        private string ConvertToText(ExcelWorksheet worksheet, int[] columnWidths)
        {
            int rowCount = worksheet.Dimension.Rows;
            int colCount = worksheet.Dimension.Columns;
            int maxLineWidth = columnWidths.Sum() + 4 * colCount;
            string horizontalLine = new string('-', maxLineWidth);
            StringBuilder textContent = new StringBuilder();

            textContent.AppendLine(horizontalLine);

            for (int col = 1; col <= colCount; col++)
            {
                int a = -(columnWidths[col - 1] + 2);
                textContent.AppendFormat("| {0," + a + "}", worksheet.Cells[1, col].Text);
            }
            textContent.AppendLine("|");
            textContent.AppendLine(horizontalLine);

            for (int row = 2; row <= rowCount; row++)
            {
                for (int col = 1; col <= colCount; col++)
                {
                    int a = -(columnWidths[col - 1] + 2);
                    textContent.AppendFormat("| {0," + a + "}", worksheet.Cells[row, col].Text);
                }
                textContent.AppendLine("|");
            }
            textContent.AppendLine(horizontalLine);

            return textContent.ToString();
        }


        /*private void WriteTextToAzureBlob(string textContent, string azureBlobConnectionString, string containerName, string blobName)
        {
            blobName = Path.ChangeExtension(blobName, ".txt");

            BlobServiceClient blobServiceClient = new BlobServiceClient(azureBlobConnectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            blobClient.Upload(new MemoryStream(Encoding.UTF8.GetBytes(textContent), true));
        }*/


        

        [NonAction]
        private async Task ConvertXlsxToTxt(string xlsxFilePath, string newFileName, ApplicationContext db)
        {
            //ExcelPackage package = GetExcelPackage(xlsxFilePath);
            ExcelPackage package = await _azureBlobStorageService.GetExcelPackageFromAzureBlob(xlsxFilePath);
            ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
            
            string textContent = ConvertToText(worksheet, CalculateColumnWidths(worksheet));
          
            try{
                string[] pathSegments = xlsxFilePath.Split('/'); // Split the path by '/'
                string fileName = pathSegments[pathSegments.Length - 1].Split('.')[0];

                string uploadedFileUri = await _azureBlobStorageService.UploadFileAsync_TO_ConvertedFiles(textContent, User.Identity.Name, fileName, db);
                if (!string.IsNullOrEmpty(uploadedFileUri)){ViewBag.Message = "File Upload Successful";}
                else{
                    ViewBag.Message = "File Upload Failed";
                    ViewBag.IsUpload = true;
                }
            }
            catch (Exception ex){}

     
            //WriteTextToAzureBlob(textContent, azureBlobConnectionString, containerName, $"{ Guid.NewGuid()}txtfile");
            
            //SaveFileToDatabase(db, newFileName, txtFilePath);

            package.Dispose();
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


        /* [NonAction]
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
         }*/



        

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