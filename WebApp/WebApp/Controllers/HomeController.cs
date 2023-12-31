﻿using Microsoft.AspNetCore.Authorization;
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

            ViewData["HostName"] = Environment.MachineName;
            return View();
        }

        public IActionResult AccessDenied() => View();
    }
}