﻿using Aspose.Cells;
using Azure.Storage.Blobs;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Diagnostics;
using System.Text;
using WebApp.Hubs;
using WebApp.Interfaces;
using WebApp.Models;

namespace WebApp.Controllers
{
    [Authorize]
    public class FileController : Controller
    {
        private static Dictionary<string, bool> IsWorking = new Dictionary<string,bool>();
        private static Dictionary<string, Dictionary<string, bool>> СancelBoolsForUser = new Dictionary<string, Dictionary<string, bool>>();



        private readonly IAzureBlobStorageService _azureBlobStorageService;
        ApplicationContext db;
        private readonly IHubContext<ProgressHub> _hubContext;
        private readonly IHubContext<jsCodeHub> _hubContext_jsCodeHub;
      


        public FileController(
            ApplicationContext context, 
            IAzureBlobStorageService azureBlobStorageService,
            IHubContext<ProgressHub> hubContext, 
            IHubContext<jsCodeHub> hubContext_jsCodeHub)
        {       
            db = context;
            _azureBlobStorageService = azureBlobStorageService;
            _hubContext = hubContext;
            _hubContext_jsCodeHub = hubContext_jsCodeHub;
        }

        public IActionResult Index()
        {
            ViewBag.DBFilesToConvert = db.FilesToConvert.Where(f => f.UserName == User.Identity.Name).ToList();
            ViewBag.DBConvertedFiles = db.ConvertedFiles.Where(f => f.UserName == User.Identity.Name).ToList();
            return View();
        }

        [Authorize(Policy = "Administrator")]
        public IActionResult IndexForAdmin()
        {
            ViewBag.DBFilesToConvert = db.FilesToConvert.ToList();
            return View();
        }





        [RequestFormLimits(MultipartBodyLengthLimit = 209715200)] // 200 MB
        [RequestSizeLimit(209715200)] // 200 MB
        [HttpPost]
        public async Task<ActionResult> Upload(IFormFile file)
        {
            var files = db.FilesToConvert.Where(f => f.UserName == User.Identity.Name).ToList();
            ViewBag.MaxNumber = "";

            int numberOfFiles = files.Count();
            try
            {
                if (numberOfFiles < 3)
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
                else
                {
                    TempData["MaxNumber"] = "maximum 3 files";
                    return RedirectToAction("Index", "file");
                }
            }
            catch (Exception ex) { ViewBag.Message = "File Upload Failed"; }

            _hubContext_jsCodeHub.Clients.All.SendAsync("ExecuteJavaScript", "location.reload();");
            return RedirectToAction("Index", "file");
        }

        [HttpPost]
        public async Task<ActionResult> Сancel()
        {
            if (User.Identity.IsAuthenticated)
            {
                string username = User.Identity.Name;

                if (!СancelBoolsForUser.ContainsKey(username))
                {
                    СancelBoolsForUser[username] = new Dictionary<string, bool>
                    {
                        { "isСanceled", false },
                        { "isUploadedOnlyToAzure", false },
                        { "isUploadedToAzureAndDB", false }
                    };
                }
            }

            if (IsWorking[User.Identity.Name] == true)
            {
                СancelBoolsForUser[User.Identity.Name]["isСanceled"] = true;
            }


            

            //_hubContext_jsCodeHub.Clients.All.SendAsync("ExecuteJavaScript", "location.reload();");
            return RedirectToAction("index");
        }

        #region convertation proccess


        [HttpPost]
        public async Task<IActionResult> Process()
        {

            // Запускаємо _Process в фоновому потоці
            bool IsAuthenticated = User.Identity.IsAuthenticated;
            string Name = User.Identity.Name;

                 
            BackgroundJob.Enqueue(() => _Process(IsAuthenticated, Name));

            return RedirectToAction("Index");
        }



        //[HttpPost]
        public async Task _Process(bool IsAuthenticated, string Name)
        {
            if (IsAuthenticated)
            {
                string username = Name;

                if (!СancelBoolsForUser.ContainsKey(username))
                {
                    СancelBoolsForUser[username] = new Dictionary<string, bool>
                    {
                        { "isСanceled", false },
                        { "isUploadedOnlyToAzure", false },
                        { "isUploadedToAzureAndDB", false }
                    };
                }


                /*if (!IsWorking.ContainsKey(username))
                {
                    IsWorking[username] = true;
                }*/
                IsWorking[username] = true;

            }

            var FilesToConvert = db.FilesToConvert.Where(f => f.UserName == Name).ToList();
            string _UserName = Name;

            int numberOfFiles = FilesToConvert.Count();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount // Adjust the degree of parallelism as needed
            };

            List<Task> tasks = new List<Task>();
            List<Task> deleteTasks = new List<Task>();

            Parallel.ForEach(FilesToConvert, parallelOptions, (item) =>
            {
                Task task = Task.Run(async () =>
                {
                    string filePath = item.FullPath;
                    string Title = item.Title.Split(".")[0];
                    Thread.Sleep(2000);
                    _hubContext.Clients.All.SendAsync("ReceiveProgress", 2, item.Title.Replace(".", "-"));

                    if (СancelBoolsForUser[Name]["isСanceled"])
                    {
                        //IsWorking[Name] = false;
                        return;
                        
                    }
             
                    bool ConvertXlsxToTxtResult = await ConvertXlsxToTxt(filePath, Title, db, item.Title.Replace(".", "-"), Name);

                    if (ConvertXlsxToTxtResult == false)
                    {
                        return;
                    }
                    else
                    {
                        lock (tasks)
                        {
                            СancelBoolsForUser[Name]["isUploadedOnlyToAzure"] = true;
                        }
                    }

                    if (СancelBoolsForUser[Name]["isСanceled"])
                    {
                        //IsWorking[Name] = false;
                        return;
                    }

                    string folderPath = $"{_UserName}/ConvertedFiles/";
                    string fileName = $"{Title}.txt";
                    User user = db.Users.SingleOrDefault(u => u.UserName == _UserName);
                   
                    lock (tasks)
                    {
                        AddFileTo_ConvertedFiles_Db(fileName, folderPath, folderPath + fileName, _UserName, user.Id.ToString(), user, db);
                        СancelBoolsForUser[Name]["isUploadedToAzureAndDB"] = true; // Помічаємо, що є хоча б один успішно добавлений файл
                    }
                    if (СancelBoolsForUser[Name]["isСanceled"])
                    {
                        //IsWorking[Name] = false;
                        return;
                    }
                    _hubContext.Clients.All.SendAsync("ReceiveProgress", 100, item.Title.Replace(".", "-"));
                
                });

                tasks.Add(task);
            });

            await Task.WhenAll(tasks);
            
            //_hubContext_jsCodeHub.Clients.All.SendAsync("ExecuteJavaScript", "location.reload();");

            if (СancelBoolsForUser[Name]["isСanceled"] && СancelBoolsForUser[Name]["isUploadedToAzureAndDB"])
            {
                var ConvertedFiles = db.ConvertedFiles.Where(f => f.UserName == Name).ToList();
                var F_ToConvert = db.FilesToConvert.Where(f => f.UserName == Name).ToList();
                foreach (var item in F_ToConvert)
                {
                    var PathToDelete = $"{Name}/ConvertedFiles/" + item.Title.Split(".")[0] + ".txt";

                    db.DeleteConvertedFilesByUserNameAndFullPath(Name.ToString(), PathToDelete);
                    
                    await _azureBlobStorageService.DeleteBlobAsync(PathToDelete);
                }

                _hubContext_jsCodeHub.Clients.All.SendAsync("ExecuteJavaScript", "location.reload();");

                СancelBoolsForUser[Name]["isСanceled"] = false;
                СancelBoolsForUser[Name]["isUploadedOnlyToAzure"] = false;
                СancelBoolsForUser[Name]["isUploadedToAzureAndDB"] = false;

                
                _hubContext_jsCodeHub.Clients.All.SendAsync("ExecuteJavaScript", "location.reload();");
                //return RedirectToAction("Index", "file");
                //return false;
                IsWorking[Name] = false;
                _hubContext_jsCodeHub.Clients.All.SendAsync("ExecuteJavaScript", "location.reload();");
                return;
            }


            if (СancelBoolsForUser[Name]["isСanceled"] && 
                СancelBoolsForUser[Name]["isUploadedOnlyToAzure"] && 
                !СancelBoolsForUser[Name]["isUploadedToAzureAndDB"])
            {
                var F_ToConvert = db.FilesToConvert.Where(f => f.UserName == Name).ToList();
                foreach (var item in F_ToConvert)
                {                   
                    var PathToDelete = $"{Name}/ConvertedFiles/" + item.Title.Split(".")[0] + ".txt";

                    if (await _azureBlobStorageService.IsPathExists(PathToDelete))
                    {
                        await _azureBlobStorageService.DeleteBlobAsync(PathToDelete);
                    }                     
                }
                _hubContext_jsCodeHub.Clients.All.SendAsync("ExecuteJavaScript", "location.reload();");

                СancelBoolsForUser[Name]["isСanceled"] = false;
                СancelBoolsForUser[Name]["isUploadedOnlyToAzure"] = false;
                СancelBoolsForUser[Name]["isUploadedToAzureAndDB"] = false;

               
                _hubContext_jsCodeHub.Clients.All.SendAsync("ExecuteJavaScript", "location.reload();");
                //return RedirectToAction("Index", "file");
                //return false;
                IsWorking[Name] = false;
                _hubContext_jsCodeHub.Clients.All.SendAsync("ExecuteJavaScript", "location.reload();");
                return;
            }

         
            if (!СancelBoolsForUser[Name]["isСanceled"])
            {
                Parallel.ForEach(FilesToConvert, parallelOptions, (item) =>
                {
                    Task deleteTask = Task.Run(async () =>
                    {
                        string filePath = item.FullPath;

                        await _azureBlobStorageService.DeleteBlobAsync(filePath);
                    });

                    deleteTasks.Add(deleteTask);
                });

                Task.WhenAll(deleteTasks).Wait();

                db.DeleteFilesToConvertByUserName(Name.ToString());


                
                _hubContext_jsCodeHub.Clients.All.SendAsync("ExecuteJavaScript", "location.reload();");
            }

            СancelBoolsForUser[Name]["isСanceled"] = false;
            СancelBoolsForUser[Name]["isUploadedOnlyToAzure"] = false;
            СancelBoolsForUser[Name]["isUploadedToAzureAndDB"] = false;

            _hubContext_jsCodeHub.Clients.All.SendAsync("ExecuteJavaScript", "location.reload();");

            //return RedirectToAction("Index", "file");
            //return true;
            IsWorking[Name] = false;
            return;
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

        private async Task<string> ConvertToText(ExcelWorksheet worksheet, int[] columnWidths, string Title, string Name)
        {
            if (СancelBoolsForUser[Name]["isСanceled"])
            {
                return null;
            }

            await _hubContext.Clients.All.SendAsync("ReceiveProgress", 10, Title);

            if (СancelBoolsForUser[Name]["isСanceled"])
            {
                return null;
            }

            int rowCount = worksheet.Dimension.Rows;
            int colCount = worksheet.Dimension.Columns;

            int maxLineWidth = columnWidths.Sum() + 4 * colCount;

            string horizontalLine = new string('-', maxLineWidth);

            StringBuilder textContent = new StringBuilder();

            if (СancelBoolsForUser[Name]["isСanceled"])
            {
                return null;
            }

            textContent.AppendLine(horizontalLine);

            for (int col = 1; col <= colCount; col++)
            {
                if (СancelBoolsForUser[Name]["isСanceled"])
                {
                    return null;
                }

                int a = -(columnWidths[col - 1] + 2);
                textContent.AppendFormat("| {0," + a + "}", worksheet.Cells[1, col].Text);
            }

            textContent.AppendLine("|");
            textContent.AppendLine(horizontalLine);

            if (СancelBoolsForUser[Name]["isСanceled"])
            {
                return null;
            }

            double progressStep = 80.0 / (rowCount - 1); 

            double progress = 10; // Початковий прогрес

            for (int row = 2; row <= rowCount; row++)
            {
                for (int col = 1; col <= colCount; col++)
                {
                    int a = -(columnWidths[col - 1] + 2);
                    textContent.AppendFormat("| {0," + a + "}", worksheet.Cells[row, col].Text);

                    if (СancelBoolsForUser[Name]["isСanceled"])
                    {
                        return null;
                    }
                }
                textContent.AppendLine("|");

                progress += progressStep; // Оновлення прогресу на кожному кроці       
                await _hubContext.Clients.All.SendAsync("ReceiveProgress", Math.Round(progress, 2), Title);
            }

            textContent.AppendLine(horizontalLine);

            if (СancelBoolsForUser[Name]["isСanceled"])
            {
                return null;
            }

            await _hubContext.Clients.All.SendAsync("ReceiveProgress", 90, Title);

            if (СancelBoolsForUser[Name]["isСanceled"])
            {
                return null;
            }
            return textContent.ToString();
        }


        [NonAction]
        private async Task<bool> ConvertXlsxToTxt(string xlsxFilePath, string newFileName, ApplicationContext db, string Title, string Name)
        {
            if (СancelBoolsForUser[Name]["isСanceled"])
            {
                return false;
            }

            using (ExcelPackage package = await _azureBlobStorageService.GetExcelPackageFromAzureBlob(xlsxFilePath))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                if (СancelBoolsForUser[Name]["isСanceled"])
                {
                    return false;
                }

                await _hubContext.Clients.All.SendAsync("ReceiveProgress", 6.5, Title);

                if (СancelBoolsForUser[Name]["isСanceled"])
                {
                    return false;
                }

                string textContent = await ConvertToText(worksheet, CalculateColumnWidths(worksheet), Title, Name);

                if (textContent == null)
                {
                    return false;
                }

                try
                {                  
                    string uploadedFileUri = await _azureBlobStorageService.UploadFileAsync_TO_ConvertedFiles(textContent, Name, newFileName, db);
                    if (!string.IsNullOrEmpty(uploadedFileUri)) 
                    { 
                        ViewBag.Message = "File Upload Successful";
                        
                        await _hubContext.Clients.All.SendAsync("ReceiveProgress", 95, Title);

                        return true; 
                    }
                    else
                    {
                        ViewBag.Message = "File Upload Failed";                      
                    }
                }
                catch (Exception ex) { }
            }
            return false;        
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

            /*connectionString
            containerName */
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
            _hubContext_jsCodeHub.Clients.All.SendAsync("ExecuteJavaScript", "location.reload();");
            return RedirectToAction("Index", "file");
        }

        [HttpPost]
        public async Task<ActionResult> DeleteUploaded(string fullpath)
        {
            db.DeleteFilesToConvertByUserNameAndFullPath(User.Identity.Name.ToString(), fullpath);

            await _azureBlobStorageService.DeleteBlobAsync(fullpath);
            _hubContext_jsCodeHub.Clients.All.SendAsync("ExecuteJavaScript", "location.reload();");
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
            _hubContext_jsCodeHub.Clients.All.SendAsync("ExecuteJavaScript", "location.reload();");
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
            _hubContext_jsCodeHub.Clients.All.SendAsync("ExecuteJavaScript", "location.reload();");
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