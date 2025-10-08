using tHerdBackend.Core.Interfaces.Abstractions;
using tHerdBackend.Infra.Helpers;
using tHerdBackend.Services.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using tHerdBackend.Composition;

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
            builder.Services.AddScoped<IImageStorage, CloudinaryImageStorage>();

            // Auth Service
            builder.Services.AddScoped<AuthService>();

			// 加入 DI 註冊（這行會自動把 Infra、Service 都綁好）
			builder.Services.AddFlexBackend(builder.Configuration);

			// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
			// Controllers & Swagger
			builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

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
