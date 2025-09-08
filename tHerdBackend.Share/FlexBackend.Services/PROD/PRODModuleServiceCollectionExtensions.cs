using FlexBackend.Core.Interfaces.PROD;
using FlexBackend.Core.Interfaces.Products;
using FlexBackend.Infra.Repository.PROD;
using Microsoft.Extensions.DependencyInjection;

namespace FlexBackend.Services.PROD
{
    public static class PRODModuleServiceCollectionExtensions
    {
        public static IServiceCollection AddPRODModule(this IServiceCollection services)
        {
            // 註冊 Service
            services.AddScoped<IProductService, ProductService>();

            // 註冊 Repository (實作在 Infra)
            services.AddScoped<IProdProductRepository, ProdProductRepository>();

            return services;
        }
    }
}
