using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApp.Hubs;
using WebApp.Interfaces;
using WebApp.Models;
using WebApp.Services;
using Hangfire.MemoryStorage;

namespace WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            builder.Services.AddHangfire(config => config.UseMemoryStorage());


            builder.Services.AddSignalR();

            builder.Services.AddTransient<IAzureBlobStorageService, AzureBlobStorageService>();
         
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



            app.UseHangfireDashboard();
            app.UseHangfireServer();




            app.UseForwardedHeaders();

            if (!app.Environment.IsDevelopment()){
                app.UseExceptionHandler("/Home/Error");

                //app.UseHsts();
            }
            else
            {
                app.UseDeveloperExceptionPage();
            }


            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            #region Add Authentication & Authorization

            app.UseAuthentication();
            app.UseAuthorization();

            #endregion

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<jsCodeHub>("/jscodeHub");
                endpoints.MapHub<ProgressHub>("/progressHub");
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");             
            });

            app.Run();
        }
    }
}