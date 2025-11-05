using tHerdBackend.Core.Interfaces.PROD;
using tHerdBackend.Core.Interfaces.Products;
using tHerdBackend.Core.Interfaces.SYS;
using tHerdBackend.Infra.Repository.PROD;
using tHerdBackend.Infra.Repository.SYS;
using Microsoft.Extensions.DependencyInjection;
using tHerdBackend.Services.PROD.API;

namespace tHerdBackend.Services.PROD
{
    public static class PRODModuleServiceCollectionExtensions
    {
        public static IServiceCollection AddPRODModule(this IServiceCollection services)
        {
            // 註冊 Service
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IProductQueryService, ProductQueryService>();
			services.AddScoped<IProductsForApiService, ProductsForApiService>();

			// 註冊 Repository (實作在 Infra)
			services.AddScoped<IProdProductRepository, ProdProductRepository>();
            services.AddScoped<IProdProductQueryRepository, ProductQueryRepository>();
            services.AddScoped<ISysCodeRepository, SysCodeRepository>();

			services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();

			MapsterConfig.Register();

            return services;
        }
    }
}
