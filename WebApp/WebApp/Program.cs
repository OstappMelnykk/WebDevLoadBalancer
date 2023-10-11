using System.Security.Claims;

namespace WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);



            builder.Services.AddAuthentication("Cookie").AddCookie("Cookie", config => {
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