using tHerdBackend.Services.ORD;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using tHerdBackend.Core.DTOs.ORD;
using tHerdBackend.Core.Interfaces.ORD;
using tHerdBackend.Infra.Repository.ORD;

namespace tHerdBackend.Services.ORD
{
    public static class ORDModuleServiceCollectionExtensions
    {
        /// <summary>
        /// 註冊 ORD 模組的所有服務
        /// </summary>
        public static IServiceCollection AddORDModule(
            this IServiceCollection services,
            IConfiguration configuration)  // 加入 configuration 參數
        {
            /// <summary>
            /// 註冊 ORD 模組的所有服務
            /// </summary>
            
            // 加入 OrderService 註冊
            services.AddScoped<OrderService>();

            // 註冊 HttpClient
            services.AddHttpClient();

            // 註冊 HttpContextAccessor
            services.AddHttpContextAccessor();


            // 1. 註冊 ECPay 設定(從 appsettings.json 讀取)
            services.Configure<ECPayConfig>(
                configuration.GetSection("ECPay"));

            // 2. 註冊 ECPay Service
            services.AddScoped<IECPayService, ECPayService>();

            // 3. 註冊 ECPay Repositories
            services.AddScoped<IEcpayNotificationRepository, EcpayNotificationRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();

            return services;
        }
    }
}