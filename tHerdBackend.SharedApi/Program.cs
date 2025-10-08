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
            builder.Services.AddScoped<IImageStorage, CloudinaryImageStorage>();

            // Auth Service
            builder.Services.AddScoped<AuthService>();

			// �[�J DI ���U�]�o��|�۰ʧ� Infra�BService ���j�n�^
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

			// �ҥ� JWT ����
			app.UseAuthentication();
			app.UseAuthorization();

			app.MapControllers();

            app.Run();
        }
    }
}
