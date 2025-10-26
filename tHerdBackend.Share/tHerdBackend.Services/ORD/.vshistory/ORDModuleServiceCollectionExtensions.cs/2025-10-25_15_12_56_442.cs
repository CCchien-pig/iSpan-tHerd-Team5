using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using tHerdBackend.Core.DTOs.ORD;
using tHerdBackend.Core.Interfaces.ORD;
using tHerdBackend.Infra.Repository.ORD;
using tHerdBackend.Services.ORD;

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
            services.AddScoped<IEcpayNotificationRepository, EcpayNotificationRepository>();

            // 3. 註冊 ECPay Repositories
            services.AddScoped<IEcpayNotificationRepository, EcpayNotificationRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();

            // 添加轉發標頭支援（for ngrok）
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                          ForwardedHeaders.XForwardedProto |
                                          ForwardedHeaders.XForwardedHost;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            return services;
        }

        // 添加模組中介軟體配置
        public static IApplicationBuilder UseORDModule(this IApplicationBuilder app)
        {
            // 使用轉發標頭
            app.UseForwardedHeaders();

            return app;
        }
    }
}