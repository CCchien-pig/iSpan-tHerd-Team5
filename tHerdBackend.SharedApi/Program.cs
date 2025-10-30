using CloudinaryDotNet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using tHerdBackend.Composition;
using tHerdBackend.Core.Abstractions;
using tHerdBackend.Core.Abstractions.Security;
using tHerdBackend.Core.DTOs.ORD;
using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.Core.Interfaces.Abstractions;
using tHerdBackend.Infra.DBSetting;
using tHerdBackend.Infra.Helpers;
using tHerdBackend.Infra.Models;
using tHerdBackend.Services.Common;
using tHerdBackend.Services.Common.Auth;
using tHerdBackend.SharedApi.Controllers.Common;
using tHerdBackend.SharedApi.Infrastructure.Auth;
using tHerdBackend.Core.Abstractions.Referral;
using tHerdBackend.SharedApi.Infrastructure.Referral;

using tHerdBackend.SharedApi.Infrastructure.Config;
using tHerdBackend.SharedApi.Infrastructure.Services;


namespace tHerdBackend.SharedApi
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);

            builder.Host.ConfigureAppConfiguration((context, config) =>
            {
                if (context.HostingEnvironment.IsDevelopment())
                {
                    config.AddUserSecrets<Program>();
                }
            });

            builder.Services.Configure<ECPaySettings>(
               builder.Configuration.GetSection("ECPay")
           );


            //�إ߳s�u�r��
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
	?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

			//與後台管理系統共用 Identity 使用者資料庫
			// === Identity 使用者資料庫（與後台共用） ===
			builder.Services.AddDbContext<ApplicationDbContext>(options =>
				options.UseSqlServer(connectionString));

			builder.Services
	.AddIdentity<ApplicationUser, ApplicationRole>(options =>
	{
		// 登入前是否必須完成信箱驗證
		options.SignIn.RequireConfirmedAccount = false;   // 開發/測試可先關掉
		options.SignIn.RequireConfirmedEmail = false;

		// （可選）密碼規則
		options.Password.RequireDigit = true;
		options.Password.RequireLowercase = true;
		options.Password.RequireUppercase = true;
		options.Password.RequireNonAlphanumeric = true;
		options.Password.RequiredLength = 8;

		// （可選）鎖定策略
		options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
		options.Lockout.MaxFailedAccessAttempts = 5;
		options.Lockout.AllowedForNewUsers = true;

		// （可選）Email 唯一
		options.User.RequireUniqueEmail = true;
	})
	.AddEntityFrameworkStores<ApplicationDbContext>()
	.AddDefaultTokenProviders();

			//關閉預設 Claims 映射
			System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
			// JWT Authentication
			builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                { // 設定驗證參數
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
					RoleClaimType = "role",
					NameClaimType = "name",
					ClockSkew = TimeSpan.FromMinutes(1),
					ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"] ?? string.Empty))
                };
                options.Events = new JwtBearerEvents // 自訂未授權回應，避免 401 回 HTML
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsync("{\"error\":\"Unauthorized\"}");
                    }
                };
				

				//除錯用
				// 在 Program.cs 的 builder.Build() 之前放一次：
				//IdentityModelEventSource.ShowPII = true; // 允許輸出 PII，方便看到真錯因

				//// 你的 .AddJwtBearer(..., options => { ... })
				//options.Events = new JwtBearerEvents
				//{
				//	OnAuthenticationFailed = ctx =>
				//	{
				//		// 會看到像「IDX10214: Audience validation failed」之類的關鍵字
				//		Console.WriteLine("JWT FAILED: " + ctx.Exception);
				//		return Task.CompletedTask;
				//	},
				//	OnChallenge = context =>
				//	{
				//		// 你已經有客製 401 回應；可以加上 log
				//		Console.WriteLine("JWT CHALLENGE: " + context.ErrorDescription);
				//		return Task.CompletedTask;
				//	}
				//};
			});

			builder.Services.AddAuthorization();

			builder.Services.AddHttpContextAccessor();
			builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
			builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
			builder.Services.AddSingleton<IReferralCodeGenerator, ReferralCodeGenerator>();
			// === SQL Connection Factory ===
			builder.Services.AddScoped<ISqlConnectionFactory>(sp => new SqlConnectionFactory(connectionString));

			// === DbContext (EF Core) ===
			builder.Services.AddDbContext<tHerdDBContext>(options =>
				options.UseSqlServer(connectionString));

			// === Session（訪客用） ===
			builder.Services.AddDistributedMemoryCache();
			builder.Services.AddSession(o =>
			{
				o.Cookie.Name = ".tHerd.Session";
				o.IdleTimeout = TimeSpan.FromDays(7);
				o.Cookie.HttpOnly = true;
				o.Cookie.SameSite = SameSiteMode.Lax;
			});

			// 允許 CORS
			builder.Services.AddCors(options =>
			{
				options.AddPolicy("AllowAll",
					policy => policy
						.AllowAnyOrigin()
						.AllowAnyMethod()
						.AllowAnyHeader());
			});

			// 前台依賴註冊Identity，註冊 CurrentUser 本體（不要掛 ICurrentUser）
			builder.Services.AddScoped<ICurrentUser, CurrentUser>();

			// Auth Service
			//builder.Services.AddScoped<AuthService>();

			// 加入 DI 註冊（這行會自動把 Infra、Service 都綁好）
			builder.Services.AddtHerdBackend(builder.Configuration);

			// Cloudinary
			builder.Services.Configure<CloudinarySettings>(
                builder.Configuration.GetSection("CloudinarySettings"));

			// 讀取設定值，建立 Cloudinary 實例
			var cloudinaryConfig = builder.Configuration.GetSection("CloudinarySettings").Get<CloudinarySettings>();
			if (cloudinaryConfig == null ||
				string.IsNullOrEmpty(cloudinaryConfig.CloudName) ||
				string.IsNullOrEmpty(cloudinaryConfig.ApiKey) ||
				string.IsNullOrEmpty(cloudinaryConfig.ApiSecret))
			{
				throw new InvalidOperationException("CloudinarySettings 未正確設定於 appsettings.json");
			}

			var account = new Account(
				cloudinaryConfig.CloudName,
				cloudinaryConfig.ApiKey,
				cloudinaryConfig.ApiSecret
			);

			// 註冊 Cloudinary 為 Singleton
			var cloudinary = new Cloudinary(account);
			builder.Services.AddSingleton(cloudinary);

			builder.Services.AddScoped<IImageStorage, CloudinaryImageStorage>();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            // Controllers & Swagger
            builder.Services.AddControllers(options =>
            {
                // ➜ 設定全域 Controller 行為，例如加入自訂路由規則
                options.Conventions.Add(new FolderBasedRouteConvention());
            })
            .AddJsonOptions(o =>
            {
                // ➜ 設定 JSON 序列化規則，例如忽略屬性大小寫
                o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "tHerd API", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "輸入 JWT Token，格式: Bearer {token}",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        new string[]{}
                    }
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
				app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

			app.UseCors("AllowAll");


			// 訪客追蹤：若無 guestId 就建立
			app.UseSession();
			app.Use(async (ctx, next) =>
			{
				if (string.IsNullOrEmpty(ctx.Session.GetString("guestId")))
					ctx.Session.SetString("guestId", Guid.NewGuid().ToString("N"));
				await next();
			});

		
			// 啟用 JWT 驗證
			app.UseAuthentication();
			app.UseAuthorization();

			app.MapControllers();

            app.Run();

            // === 讀取綠界設定值 ===
            builder.Services.Configure<ECPaySettings>(
		    builder.Configuration.GetSection("ECPay"));
            builder.Services.AddScoped<ECPayService>();

        }
    }
}
