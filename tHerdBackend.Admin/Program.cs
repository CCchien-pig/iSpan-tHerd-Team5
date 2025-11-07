using CloudinaryDotNet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using tHerdBackend.Admin.Infrastructure.Auth;
using tHerdBackend.Composition;
using tHerdBackend.Core.Abstractions;
using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.CS.Rcl.Areas.CS.Controllers;
using tHerdBackend.Infra;
using tHerdBackend.Infra.Models;
using tHerdBackend.Services.USER;
using tHerdBackend.UIKit.Rcl;
using tHerdBackend.USER.Rcl;

namespace tHerdBackend.Admin
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

			builder.Services.AddDefaultIdentity<ApplicationUser>(options => { options.SignIn.RequireConfirmedAccount = true; options.SignIn.RequireConfirmedEmail = false; })
				.AddRoles<ApplicationRole>()
				.AddEntityFrameworkStores<ApplicationDbContext>()
				.AddDefaultTokenProviders();



			//確保預設驗證方案是 Identity Cookie（避免被其他套件改成 JWT）
			//builder.Services.AddAuthentication(options =>
			//{
			//	options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme; // "Identity.Application"
			//	options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
			//	options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
			//});
			// === JWT + Cookie 雙驗證方案 ===
			builder.Services.AddAuthentication(options =>
			{
				// 預設驗證使用 Cookie（後台登入）
				options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
				options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
			})
			// ⚠️ 不要放在 AddDefaultIdentity 下面，這是獨立的！
			.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
			{
				options.Events = new JwtBearerEvents
				{
					OnMessageReceived = context =>
					{
						// ✅ 允許 SignalR 從 QueryString 帶 Token
						var accessToken = context.Request.Query["access_token"];
						var path = context.HttpContext.Request.Path;
						if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
							context.Token = accessToken;

						return Task.CompletedTask;
					},
					OnChallenge = context =>
					{
						context.HandleResponse();
						context.Response.StatusCode = StatusCodes.Status401Unauthorized;
						context.Response.ContentType = "application/json";
						return context.Response.WriteAsync("{\"error\":\"Unauthorized\"}");
					}
				};

				var cfg = builder.Configuration.GetSection("Jwt");
				var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["SigningKey"]!));
				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = key,
					ValidateIssuer = true,
					ValidIssuer = cfg["Issuer"],
					ValidateAudience = true,
					ValidAudience = cfg["Audience"],
					ValidateLifetime = true,
					ClockSkew = TimeSpan.Zero
				};
			});
			// ✅ 新增這段：允許 Cookie 驗證（給後台 Identity 登入者）
			//.AddCookie(IdentityConstants.ApplicationScheme, options =>
			//{
			//	options.LoginPath = "/Account/Login";
			//	options.AccessDeniedPath = "/Account/AccessDenied";
			//	options.Cookie.SameSite = SameSiteMode.None;             // 跨網域要 None
			//	options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS 才能送 Cookie
			//});

			// ✅ 設定應用程式 Cookie（同上）
			//builder.Services.ConfigureApplicationCookie(options =>
			//{
			//	options.Cookie.SameSite = SameSiteMode.None;
			//	options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
			//});

			// 電子郵件服務
			builder.Services.AddTransient<IEmailSender, EmailSender>();

			builder.Services.AddScoped<tHerdBackend.USER.Rcl.Services.UserService>();

			var connStr = builder.Configuration.GetConnectionString("THerdDB")!;

            // 註冊資料存取 (Infrastructure: Dapper + EF Core)
            builder.Services.AddFlexInfra(connStr);

            // 註冊方案級服務 (Composition: 各模組的 Service + Repository)
            builder.Services.AddtHerdBackend(builder.Configuration);

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
				options.SignIn.RequireConfirmedEmail = false;
			});
			builder.Services.ConfigureApplicationCookie(options =>
			{
				options.Cookie.HttpOnly = true;
				options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
				//options.Cookie.SameSite = SameSiteMode.Lax;
				options.Cookie.SameSite = SameSiteMode.None; // ⚠️ 必須是 None 才能跨網域帶 Cookie
				options.ExpireTimeSpan = TimeSpan.FromDays(14);
				options.LoginPath = "/Identity/Account/AdminLogin";
				options.AccessDeniedPath = "/Identity/Account/AccessDenied";
				options.SlidingExpiration = true;
			});

			// 使用自訂的 ClaimsPrincipalFactory 來加入額外的 Claims
			builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, AppClaimsPrincipalFactory>();
			builder.Services.AddHttpContextAccessor();
			builder.Services.AddScoped<ICurrentUser, CurrentUser>();
			//Data Protection 金鑰持久化（避免重啟就讓 Cookie 失效）
			builder.Services.AddDataProtection()
				.PersistKeysToFileSystem(new DirectoryInfo(
					Path.Combine(builder.Environment.ContentRootPath, "dp-keys")))
				.SetApplicationName("tHerdBackend");

            // 讀取 Cloudinary 設定
            var cloudinarySettings = builder.Configuration.GetSection("CloudinarySettings");

            var account = new Account(
                builder.Configuration["CloudinarySettings:CloudName"],
                builder.Configuration["CloudinarySettings:ApiKey"],
                builder.Configuration["CloudinarySettings:ApiSecret"]
            );

            var cloudinary = new Cloudinary(account) { Api = { Secure = true } };
            builder.Services.AddSingleton(cloudinary);

			builder.Services.AddCors(options =>
			{
				options.AddPolicy("AllowFrontend", policy =>
				{
					policy.WithOrigins(
						"https://localhost:7157", // 自己 Admin
						"https://localhost:5173"  // 前台 Vue
					)
					.AllowAnyHeader()
					.AllowAnyMethod()
					.AllowCredentials();
				});
			});

			// 全站預設需要登入 
			var mvc = builder.Services.AddControllersWithViews(options =>
			{
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

			//	// 先套遷移，確保表存在
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

			app.UseCors("AllowFrontend");

			if (app.Environment.IsDevelopment())
			{
				app.Use(async (context, next) =>
				{
					if (!context.User.Identity?.IsAuthenticated ?? true)
					{
						var userMgr = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
						var signInMgr = context.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
						var superAdmin = await userMgr.FindByEmailAsync("superadmin@ispan.com");

						if (superAdmin != null)
						{
							await signInMgr.SignInAsync(superAdmin, isPersistent: true);
							Console.WriteLine("✅ Auto signed in as SuperAdmin (Dev Mode)");
						}
					}
					await next();
				});
			}

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

			// === 自動建立 SuperAdmin 帳號 ===
			using (var scope = app.Services.CreateScope())
			{
				var services = scope.ServiceProvider;
				var userMgr = services.GetRequiredService<UserManager<ApplicationUser>>();
				var roleMgr = services.GetRequiredService<RoleManager<ApplicationRole>>();
				var cfg = builder.Configuration;

				// 1️⃣ 建立角色
				var roles = new[] { "SuperAdmin", "Admin", "Manager" };
				foreach (var role in roles)
				{
					if (!await roleMgr.RoleExistsAsync(role))
					{
						await roleMgr.CreateAsync(new ApplicationRole { Name = role });
						Console.WriteLine($"✅ Created role: {role}");
					}
				}

				// 2️⃣ 建立超級管理員帳號
				var email = cfg["SuperAdminAccount"];
				var password = cfg["SuperAdminPassword"];

				var superAdmin = await userMgr.FindByEmailAsync(email);
				if (superAdmin == null)
				{
					superAdmin = new ApplicationUser
					{
						UserName = email,
						Email = email,
						EmailConfirmed = true,
						LastName = "系統",
						FirstName = "管理員",
						UserNumberId = 1,
						IsActive = true,
						CreatedDate = DateTime.UtcNow,
					};
					var result = await userMgr.CreateAsync(superAdmin, password);
					if (result.Succeeded)
					{
						Console.WriteLine($"✅ Created SuperAdmin: {email}");
						await userMgr.AddToRoleAsync(superAdmin, "SuperAdmin");
					}
					else
					{
						Console.WriteLine($"⚠️ Create SuperAdmin failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
					}
				}
				else
				{
					Console.WriteLine($"ℹ️ SuperAdmin already exists: {email}");
					if (!await userMgr.IsInRoleAsync(superAdmin, "SuperAdmin"))
					{
						await userMgr.AddToRoleAsync(superAdmin, "SuperAdmin");
						Console.WriteLine($"✅ Re-added SuperAdmin role to existing user");
					}
				}
			}

			app.Run();
        }
    }
}
