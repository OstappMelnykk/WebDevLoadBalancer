using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using WebApp.Models;

namespace WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);






			string connection = builder.Configuration.GetConnectionString("DefaultConnection");


			builder.Services
                .AddDbContext<ApplicationContext>(options => options.UseNpgsql(connection));

			builder.Services
                .AddIdentity<User, IdentityRole>(config =>
                {
                    config.Password.RequireDigit = false;
                    config.Password.RequireLowercase = false;
                    config.Password.RequireNonAlphanumeric = false;
                    config.Password.RequireUppercase = false;
                    config.Password.RequiredLength = 4;
                })
                .AddEntityFrameworkStores<ApplicationContext>();




            /*builder.Services.AddAuthentication("Cookie").AddCookie("Cookie", config => {
                config.LoginPath = "/Account/Login";
                config.AccessDeniedPath = "/Home/AccessDenied";

            });*/

            builder.Services.ConfigureApplicationCookie(config =>
            {
                config.LoginPath = "/Account/Login";
                config.AccessDeniedPath = "/Home/AccessDenied";
            });



            builder.Services.AddAuthorization(options => {
                options.AddPolicy("User", b => b.RequireAssertion(x =>
                                                x.User.HasClaim(ClaimTypes.Role, "User") ||
                                                x.User.HasClaim(ClaimTypes.Role, "Administrator")));


                options.AddPolicy("Administrator", b => b.RequireAssertion(x =>
                                                x.User.HasClaim(ClaimTypes.Role, "Administrator")));
            });








            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment()){
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();



            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}