using tHerdBackend.Services.ORD;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using tHerdBackend.Core.DTOs.ORD;
using tHerdBackend.Core.Interfaces.ORD;
using tHerdBackend.Infra.Repository.ORD;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;

namespace tHerdBackend.Services.ORD
{
    public static class ORDModuleServiceCollectionExtensions
    {
        /// <summary>
        /// 註冊 ORD 模組的所有服務
        /// </summary>
        public static IServiceCollection AddORDModule(
            this IServiceCollection services,
            IConfiguration configuration)
        {
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

            // 4. 配置轉發標頭支援（for ngrok and reverse proxy）
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                          ForwardedHeaders.XForwardedProto |
                                          ForwardedHeaders.XForwardedHost;
                // 清除已知的網路和代理限制，允許所有來源
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
                // 限制轉發數量避免濫用
                options.ForwardLimit = 2;
            });

            return services;
        }

        /// <summary>
        /// 使用 ORD 模組中介軟體
        /// </summary>
        public static IApplicationBuilder UseORDModule(this IApplicationBuilder app)
        {
            // 1. 使用轉發標頭（必須在其他中介軟體之前）
            app.UseForwardedHeaders();

            // 2. 使用 ORD 診斷中介軟體
            app.UseORDDiagnostic();

            return app;
        }
    }
}