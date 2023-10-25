using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
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
        private readonly IAzureBlobStorageService _azureBlobStorageService;
        ApplicationContext db;

        public FileController(ApplicationContext context, IAzureBlobStorageService azureBlobStorageService)
        {       
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
                if (!string.IsNullOrEmpty(uploadedFileUri)) ViewBag.Message = "File Upload Successful";
                else
                {
                    ViewBag.Message = "File Upload Failed";
                    ViewBag.IsUpload = true;
                }

            }
            catch (Exception ex) { ViewBag.Message = "File Upload Failed"; }


            return RedirectToAction("Index", "file");
        }

        #region convertation proccess
      
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
        
        [NonAction]
        private async Task ConvertXlsxToTxt(string xlsxFilePath, string newFileName, ApplicationContext db)
        {
            ExcelPackage package = await _azureBlobStorageService.GetExcelPackageFromAzureBlob(xlsxFilePath);
            ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
            
            string textContent = ConvertToText(worksheet, CalculateColumnWidths(worksheet));
          
            try{
                string[] pathSegments = xlsxFilePath.Split('/'); 
                string fileName = pathSegments[pathSegments.Length - 1].Split('.')[0];

                string uploadedFileUri = await _azureBlobStorageService.UploadFileAsync_TO_ConvertedFiles(textContent, User.Identity.Name, fileName, db);
                if (!string.IsNullOrEmpty(uploadedFileUri)){ViewBag.Message = "File Upload Successful";}
                else{
                    ViewBag.Message = "File Upload Failed";
                    ViewBag.IsUpload = true;
                }
            }
            catch (Exception ex){}

            package.Dispose();
        }

        #endregion

        #region  Addition files to DB

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

        #endregion

        #region cliend Download      
        [HttpPost]
        public async Task<ActionResult> Download(string blobPath)
        {
            var blobServiceClient = new BlobServiceClient("DefaultEndpointsProtocol=https;AccountName=webdevblobstorage111;AccountKey=34UT1RhvVbZySCEFwnvUjB6QWytyioSSE2dM3X5XPPS/riC1AhzmmckT+hlCRLn4JRCwAynDQY+4+AStSDlBmg==;EndpointSuffix=core.windows.net");
            var blobContainerClient = blobServiceClient.GetBlobContainerClient("webdev");
            var blobClient = blobContainerClient.GetBlobClient(blobPath);

            if (!await blobClient.ExistsAsync())
                return NotFound();

            var response = await blobClient.OpenReadAsync();
            var content = response;
            var fileName = Path.GetFileName(blobPath);

            return File(content, "application/octet-stream", fileName);
        }
        #endregion

        #region Delete
        [HttpPost]
        public async Task<ActionResult> DeleteConverted(string fullpath)
        {
            db.DeleteConvertedFilesByUserNameAndFullPath(User.Identity.Name.ToString(), fullpath);
            await _azureBlobStorageService.DeleteBlobAsync(fullpath);

            return RedirectToAction("Index", "file");
        }

        [HttpPost]
        public async Task<ActionResult> DeleteUploaded(string fullpath)
        {
            db.DeleteFilesToConvertByUserNameAndFullPath(User.Identity.Name.ToString(), fullpath);
            await _azureBlobStorageService.DeleteBlobAsync(fullpath);
            return RedirectToAction("Index", "file");
        }

        [HttpPost]
        public async Task<ActionResult> Delete_All_Converted()
        {
            var DBConvertedFiles = db.ConvertedFiles.Where(f => f.UserName == User.Identity.Name).ToList();

            foreach (var item in DBConvertedFiles)
            {
                string fullpath = item.FullPath;
                await _azureBlobStorageService.DeleteBlobAsync(fullpath);
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
                await _azureBlobStorageService.DeleteBlobAsync(fullpath);
            }
            db.DeleteFilesToConvertByUserName(User.Identity.Name.ToString());

            return RedirectToAction("Index", "file");
        }
        #endregion

        #region MeasureTime

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
        #endregion
    }
}