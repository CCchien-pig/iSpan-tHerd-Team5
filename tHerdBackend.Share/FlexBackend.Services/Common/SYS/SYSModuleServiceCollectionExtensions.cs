using FlexBackend.Core.Interfaces.SYS;
using FlexBackend.Infra.Repository.SYS;
using Microsoft.Extensions.DependencyInjection;

namespace FlexBackend.Services.PROD
{
    public static class SYSModuleServiceCollectionExtensions
    {
        public static IServiceCollection AddSYSModule(this IServiceCollection services)
        {
            // 註冊 Service
            //services.AddScoped<IProductService, ProductService>();

            // 註冊 Repository (實作在 Infra)
            services.AddScoped<ISysProgramConfigRepository, SysProgramConfigRepository>();

            return services;
        }
    }
}
