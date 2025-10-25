using Microsoft.Extensions.DependencyInjection;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Infra.Repository.SUP;

namespace tHerdBackend.Services.SUP
{
    public static class SUPModuleServiceCollectionExtensions
    {
        public static IServiceCollection AddSUPModule(this IServiceCollection services)
        {
			services.AddAntiforgery();

			// 註冊 Service
			services.AddScoped<ISupplierService, SupplierService>();

			services.AddScoped<IStockService, StockService>();
			services.AddScoped<IStockBatchService, StockBatchService>();

			services.AddScoped<ILogisticsService, LogisticsService>();
			services.AddScoped<ILogisticsRateService, LogisticsRateService>();

			services.AddScoped<IBrandService, BrandService>();


			// 註冊 Repository
			services.AddScoped<ISupplierRepository, SupplierRepository>();

			services.AddScoped<ILogisticsRepository, LogisticsRepository>();
			services.AddScoped<ILogisticsRateRepository, LogisticsRateRepository>();

			services.AddScoped<IShippingFeeRepository, ShippingFeeRepository>();
			
			services.AddScoped<IBrandRepository, BrandRepository>();
			
			return services;
        }
    }
}
