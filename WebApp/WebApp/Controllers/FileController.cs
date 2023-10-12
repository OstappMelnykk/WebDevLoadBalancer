using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;

namespace WebApp.Controllers
{
    [Authorize]
    public class FileController : Controller
    {

        /*FileContext _context;
        IWebHostEnvironment _appEnvironment;

        public FileController(FileContext context, IWebHostEnvironment appEnvironment)
        {
            _context = context;
            _appEnvironment = appEnvironment;
        }*/







        /*[Authorize(Policy = "User")]
        public IActionResult UserMainPage()
        {
            ViewBag.Name = User.Identity.Name;
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            return View(_context.Files.ToList());
        }

        [Authorize(Policy = "Administrator")]
        public IActionResult AdministratorMainPage()
        {
            ViewBag.Name = User.Identity.Name;
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            return View();
        }*/
    }
}
