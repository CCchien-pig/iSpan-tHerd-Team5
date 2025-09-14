using FlexBackend.Composition;
using FlexBackend.CS.Rcl.Areas.CS.Controllers;
using FlexBackend.Infra;
using FlexBackend.Services.USER;
using FlexBackend.UIKit.Rcl;
using FlexBackend.USER.Rcl;
using FlexBackend.Infra.Models;
using FlexBackend.Core.DTOs.USER;
using FlexBackend.USER.Rcl.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;

namespace FlexBackend.Admin
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString, sql =>
		sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
				.AddRoles<ApplicationRole>()
				.AddEntityFrameworkStores<ApplicationDbContext>();

			//確保預設驗證方案是 Identity Cookie（避免被其他套件改成 JWT）
			builder.Services.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme; // "Identity.Application"
				options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
				options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
			});

			builder.Services.AddTransient<IEmailSender, EmailSender>();

			builder.Services.AddScoped<FlexBackend.USER.Rcl.Services.UserService>();

			var connStr = builder.Configuration.GetConnectionString("THerdDB")!;

            // 註冊資料存取 (Infrastructure: Dapper + EF Core)
            builder.Services.AddFlexInfra(connStr);

            // 註冊方案級服務 (Composition: 各模組的 Service + Repository)
            builder.Services.AddFlexBackend(builder.Configuration);

			//secrets.json 的設定綁定到 SmtpSettings 類別
			builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

			// Identity options configuration

			builder.Services.Configure<IdentityOptions>(options =>
			{
				options.Password.RequireDigit = true;
				options.Password.RequireLowercase = true;
				options.Password.RequireNonAlphanumeric = true;
				options.Password.RequireUppercase = true;
				options.Password.RequiredLength = 8;
				options.Password.RequiredUniqueChars = 1;

				options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
				options.Lockout.MaxFailedAccessAttempts = 3;
				options.Lockout.AllowedForNewUsers = true;

				options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
				options.User.RequireUniqueEmail = true;
				options.SignIn.RequireConfirmedEmail = true;
			});
			builder.Services.ConfigureApplicationCookie(options =>
			{
				options.Cookie.HttpOnly = false;
				options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
				options.ExpireTimeSpan = TimeSpan.FromMinutes(14);
				options.LoginPath = "/Identity/Account/AdminLogin";
				options.AccessDeniedPath = "/Identity/Account/AccessDenied";
				options.SlidingExpiration = true;
			});


			//Data Protection 金鑰持久化（避免重啟就讓 Cookie 失效）
			builder.Services.AddDataProtection()
				.PersistKeysToFileSystem(new DirectoryInfo(
					Path.Combine(builder.Environment.ContentRootPath, "dp-keys")))
				.SetApplicationName("FlexBackend");

			// 全站預設需要登入 
			var mvc = builder.Services.AddControllersWithViews(options => {
				var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
				.Build(); options.Filters.Add(new AuthorizeFilter(policy));
			}).AddApplicationPart(typeof(UiKitRclMarker).Assembly)
			  .AddApplicationPart(typeof(DashboardController).Assembly);
			//----------------------------------------

			//新增Razor Pages 規約，允許特定頁面匿名
			builder.Services.AddRazorPages(options =>
			{
				options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/AdminLogin");
				options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/ForgotPassword");
				options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/ResendEmailConfirmation");
				options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/ResetPassword");
			});

			var app = builder.Build();
			//Create a scope to run the initialization(只有第一次執行帶入假資料用到)
			//using (var scope = app.Services.CreateScope())
			//{
			//	var sp = scope.ServiceProvider;

			//	// ★ 先套遷移，確保表存在
			//	var db = sp.GetRequiredService<ApplicationDbContext>();
			//	await db.Database.MigrateAsync();

			//	var logger = sp.GetRequiredService<ILogger<Program>>();
			//	logger.LogInformation("Seeding against DB: {Db}", db.Database.GetDbConnection().Database);

			//	await RoleInitializer.SeedRolesAsync(sp);
			//	await UserInitializer.SeedUsersAsync(sp);
			//}

			

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

			app.UseAuthentication();

			app.UseAuthorization();

            // 讓屬性路由（含 API）生效
            app.MapControllers();

            app.MapControllerRoute(
            name: "areas",
            pattern: "{area=CS}/{controller=Dashboard}/{action=Index}/{id?}");

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
