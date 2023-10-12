using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        [Authorize(Policy = "User")]
        public IActionResult UserMainPage()
        {
            ViewBag.Name = User.Identity.Name;
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            return View(); 
        }

        
    }
}
