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
            db.DeleteFromFilesToConvet();
            db.DeleteFromFilesAlreadyConverted();
            ViewBag.Name = User.Identity.Name;
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            return View();
        }

        public IActionResult AccessDenied() => View();
    }
}