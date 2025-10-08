using FlexBackend.Services.ORD;
using Microsoft.Extensions.DependencyInjection;

namespace FlexBackend.Services.ORD
{
    public static class ORDModuleServiceCollectionExtensions
    {
        public static IServiceCollection AddORDModule(this IServiceCollection services)
        {

            // 加入 OrderService 註冊
            services.AddScoped<OrderService>();

            return services;
        }
    }
}
