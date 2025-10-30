using tHerdBackend.Services.ORD;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using tHerdBackend.Core.DTOs.ORD;
using tHerdBackend.Core.Interfaces.ORD;
using tHerdBackend.Infra.Repository.ORD;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Services.ORD
{
    public static class ORDModuleServiceCollectionExtensions
    {
        public static IServiceCollection AddORDModule(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddScoped<OrderService>();
            services.AddHttpClient();
            services.AddHttpContextAccessor();

            // ⚡ ORD 模組專用 DbContext - 只設定 Timeout,不用重試策略
            services.AddScoped(sp =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<tHerdDBContext>();
                var connectionString = configuration.GetConnectionString("DefaultConnection");

                optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.CommandTimeout(120);  // ✅ 延長 timeout
                });

                return new tHerdDBContext(optionsBuilder.Options);
            });

            services.Configure<ECPayConfigDTO>(configuration.GetSection("ECPay"));
            services.AddScoped<IECPayService, ECPayService>();
            services.AddScoped<IEcpayNotificationRepository, EcpayNotificationRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();

            return services;
        }
    }
}