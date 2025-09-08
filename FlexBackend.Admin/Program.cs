using FlexBackend.Admin.Data;
using FlexBackend.Composition;
using FlexBackend.Infra;
using FlexBackend.UIKit.Rcl;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FlexBackend.Admin
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>();


            var connStr = builder.Configuration.GetConnectionString("THerdDB")!;

            // 註冊資料存取 (Infrastructure: Dapper + EF Core)
            builder.Services.AddFlexInfra(connStr);

            // 註冊方案級服務 (Composition: 各模組的 Service + Repository)
            builder.Services.AddFlexBackend(builder.Configuration);

            var mvc = builder.Services
	            .AddControllersWithViews()
	            .AddApplicationPart(typeof(UiKitRclMarker).Assembly);

			var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
            name: "areas",
            pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            // 如果找不到任何路由，就導向 NotFound 頁面
            app.MapFallback(() => Results.Redirect("/404"));

            app.Run();
        }
    }
}
