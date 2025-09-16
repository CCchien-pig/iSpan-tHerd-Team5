using FlexBackend.Core.Interfaces.PROD;
using FlexBackend.Core.Interfaces.Products;
using FlexBackend.Core.Interfaces.SYS;
using FlexBackend.Infra.Repository.PROD;
using FlexBackend.Infra.Repository.SYS;
using Microsoft.Extensions.DependencyInjection;

namespace FlexBackend.Services.PROD
{
    public static class PRODModuleServiceCollectionExtensions
    {
        public static IServiceCollection AddPRODModule(this IServiceCollection services)
        {
            // 註冊 Service
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IProductQueryService, ProductQueryService>();

            // 註冊 Repository (實作在 Infra)
            services.AddScoped<IProdProductRepository, ProdProductRepository>();
            services.AddScoped<IProdProductQueryRepository, ProductQueryRepository>();
            services.AddScoped<ISysCodeRepository, SysCodeRepository>();

            MapsterConfig.Register();

            return services;
        }
    }
}
