using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
    [Authorize]
    public class FileController : Controller
    {
        [Authorize(Policy = "User")]
        public IActionResult UserMainPage()
        {
            ViewBag.Name = User.Identity.Name;
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            return View();
        }

        [Authorize(Policy = "Administrator")]
        public IActionResult AdministratorMainPage()
        {
            ViewBag.Name = User.Identity.Name;
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            return View();
        }
    }
}
