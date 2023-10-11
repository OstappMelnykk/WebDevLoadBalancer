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

            #region Connection to DB

            
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


            #endregion

            #region Configuration of Authentication & Authorization
            
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

            #endregion

            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment()){
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            #region Add Authentication & Authorization

            app.UseAuthentication();
            app.UseAuthorization();

            #endregion

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}