using CloudinaryDotNet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using tHerdBackend.Composition;
using tHerdBackend.Core.Abstractions;
using tHerdBackend.Core.Interfaces.Abstractions;
using tHerdBackend.Infra.DBSetting;
using tHerdBackend.Infra.Helpers;
using tHerdBackend.Infra.Models;
using tHerdBackend.Services.Common;
using tHerdBackend.SharedApi.Controllers.Common;
using tHerdBackend.SharedApi.Infrastructure.Auth;


namespace tHerdBackend.SharedApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

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
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? string.Empty))
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

			// === SQL Connection Factory ===
			var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
			builder.Services.AddScoped<ISqlConnectionFactory>(sp => new SqlConnectionFactory(connectionString));

			// === DbContext (EF Core) ===
			builder.Services.AddDbContext<tHerdDBContext>(options =>
				options.UseSqlServer(connectionString));

            // 前台沒有 Identity，只註冊 CurrentUser 本體（不要掛 ICurrentUser）
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUser, CurrentUser>();

            // Auth Service
            builder.Services.AddScoped<AuthService>();

            // 加入 DI 註冊（這行會自動把 Infra、Service 都綁好）
            builder.Services.AddtHerdBackend(builder.Configuration);

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
                        new string[] {}
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

			// 啟用 JWT 驗證
			app.UseAuthentication();
			app.UseAuthorization();

			app.MapControllers();

            app.Run();
        }
    }
}
