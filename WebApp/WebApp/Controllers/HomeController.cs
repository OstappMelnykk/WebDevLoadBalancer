using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebApp.Interfaces;
using WebApp.Models;

namespace WebApp.Controllers
{ 
    public class HomeController : Controller
    {
        ApplicationContext db;

        public HomeController(ApplicationContext context)
        {      
            db = context;
        }

        public IActionResult Index()
        {
            ViewBag.Name = User.Identity.Name;
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;

            var host = HttpContext.Request.Host.Host;
            var port = HttpContext.Request.Host.Port;
            var scheme = HttpContext.Request.Scheme;

            ViewData["Host"] = host;
            ViewData["Port"] = port;
            ViewData["Scheme"] = scheme;

            return View();
        }

        public IActionResult AccessDenied() => View();
    }
}