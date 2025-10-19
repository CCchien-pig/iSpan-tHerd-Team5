using CloudinaryDotNet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using tHerdBackend.Composition;
using tHerdBackend.Core.Abstractions;
using tHerdBackend.Core.DTOs.USER;
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

			//�P��x�޲z�t�Φ@�� Identity �ϥΪ̸�Ʈw
			builder.Services.AddDbContext<ApplicationDbContext>(...); // �P Admin �P�@�ӨϥΪ̸�Ʈw
			builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
				.AddEntityFrameworkStores<ApplicationDbContext>()
				.AddDefaultTokenProviders();

			// JWT Authentication
			builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                { // �]�w���ҰѼ�
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? string.Empty))
                };
                options.Events = new JwtBearerEvents // �ۭq�����v�^���A�קK 401 �^ HTML
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

            // ���\ CORS
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

			// Ū���]�w�ȡA�إ� Cloudinary ���
			var cloudinaryConfig = builder.Configuration.GetSection("CloudinarySettings").Get<CloudinarySettings>();
			if (cloudinaryConfig == null ||
				string.IsNullOrEmpty(cloudinaryConfig.CloudName) ||
				string.IsNullOrEmpty(cloudinaryConfig.ApiKey) ||
				string.IsNullOrEmpty(cloudinaryConfig.ApiSecret))
			{
				throw new InvalidOperationException("CloudinarySettings �����T�]�w�� appsettings.json");
			}

			var account = new Account(
				cloudinaryConfig.CloudName,
				cloudinaryConfig.ApiKey,
				cloudinaryConfig.ApiSecret
			);

			// ���U Cloudinary �� Singleton
			var cloudinary = new Cloudinary(account);
			builder.Services.AddSingleton(cloudinary);

			builder.Services.AddScoped<IImageStorage, CloudinaryImageStorage>();

			// === SQL Connection Factory ===
			var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
			builder.Services.AddScoped<ISqlConnectionFactory>(sp => new SqlConnectionFactory(connectionString));

			// === DbContext (EF Core) ===
			builder.Services.AddDbContext<tHerdDBContext>(options =>
				options.UseSqlServer(connectionString));

            // �e�x�S�� Identity�A�u���U CurrentUser ����]���n�� ICurrentUser�^
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUser, CurrentUser>();

            // Auth Service
            builder.Services.AddScoped<AuthService>();

            // �[�J DI ���U�]�o��|�۰ʧ� Infra�BService ���j�n�^
            builder.Services.AddtHerdBackend(builder.Configuration);

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            // Controllers & Swagger
            builder.Services.AddControllers(options =>
            {
                options.Conventions.Add(new FolderBasedRouteConvention());
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "tHerd API", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "��J JWT Token�A�榡: Bearer {token}",
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

			// �ҥ� JWT ����
			app.UseAuthentication();
			app.UseAuthorization();

			app.MapControllers();

            app.Run();
        }
    }
}
