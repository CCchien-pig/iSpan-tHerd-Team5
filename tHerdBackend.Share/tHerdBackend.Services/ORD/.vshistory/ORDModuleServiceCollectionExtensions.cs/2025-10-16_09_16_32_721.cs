using FlexBackend.Services.ORD;
using Microsoft.Extensions.DependencyInjection;

namespace FlexBackend.Services.ORD
{
    public static class ORDModuleServiceCollectionExtensions
    {
        /// <summary>
        /// 註冊 ORD 模組的所有服務
        /// </summary>
        public static IServiceCollection AddORDModule(this IServiceCollection services)
        {

            // 加入 OrderService 註冊
            services.AddScoped<OrderService>();

            // 註冊 HttpClient
            services.AddHttpClient();

            // 註冊 HttpContextAccessor
            services.AddHttpContextAccessor();


            return services;
        }
    }
}
