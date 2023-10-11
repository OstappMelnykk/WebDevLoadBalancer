using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApp.Models.ViewModels;

namespace WebApp.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        public IActionResult LoginAction() => RedirectToAction("index", "home");


        [AllowAnonymous]
        public IActionResult Login(string returnUrl) => View();

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var claims = new List<Claim>
            {

                new Claim(ClaimTypes.Name, model.UserName),
                new Claim(ClaimTypes.Role, "Administrator")

                /*new Claim(ClaimTypes.Role, "User")*/
            };


            var claimIdentity = new ClaimsIdentity(claims, "Cookie");
            var claimPrincipal = new ClaimsPrincipal(claimIdentity);
            await HttpContext.SignInAsync("Cookie", claimPrincipal);

            return Redirect(model.ReturnUrl);
        }


        public IActionResult LogOff()
        {
            HttpContext.SignOutAsync("Cookie");
            return Redirect("/Home/Index");
        }
    }
}
