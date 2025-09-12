using FlexBackend.Composition;
using FlexBackend.Infra;
using FlexBackend.Services.USER;
using FlexBackend.UIKit.Rcl;
using FlexBackend.USER.Rcl;
using FlexBackend.USER.Rcl.Data;
using FlexBackend.USER.Rcl.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
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
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
				.AddRoles<ApplicationRole>()
				.AddEntityFrameworkStores<ApplicationDbContext>();

			builder.Services.AddTransient<IEmailSender, EmailSender>();

			builder.Services.AddScoped<FlexBackend.USER.Rcl.Services.UserService>();

			var connStr = builder.Configuration.GetConnectionString("THerdDB")!;

            // 註冊資料存取 (Infrastructure: Dapper + EF Core)
            builder.Services.AddFlexInfra(connStr);

            // 註冊方案級服務 (Composition: 各模組的 Service + Repository)
            builder.Services.AddFlexBackend(builder.Configuration);

            var mvc = builder.Services
	            .AddControllersWithViews()
	            .AddApplicationPart(typeof(UiKitRclMarker).Assembly);

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
				options.Cookie.HttpOnly = true;
				options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
				options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
				options.LoginPath = "/Identity/Account/Login";
				options.AccessDeniedPath = "/Identity/Account/AccessDenied";
				options.SlidingExpiration = true;
			});

			//----------------------------------------

			var app = builder.Build();

			//Create a scope to run the initialization
			using (var scope = app.Services.CreateScope())
			{
				var sp = scope.ServiceProvider;

				// ★ 先確保 schema 已到位
				var db = sp.GetRequiredService<ApplicationDbContext>();
				await db.Database.MigrateAsync();

				// 可視化：印出實際使用的 DB 名稱，避免套到錯庫
				var logger = sp.GetRequiredService<ILogger<Program>>();
				logger.LogInformation("Seeding against DB: {DbName}", db.Database.GetDbConnection().Database);

				await RoleInitializer.SeedRolesAsync(sp);
				await UserInitializer.SeedUsersAsync(sp);
			}

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
