using Microsoft.Extensions.DependencyInjection;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Infra.Repository.SUP;

namespace tHerdBackend.Services.SUP
{
    public static class SUPModuleServiceCollectionExtensions
    {
        public static IServiceCollection AddSUPModule(this IServiceCollection services)
        {
			// 註冊 Service
			services.AddScoped<IStockBatchService, StockBatchService>();
			services.AddScoped<IStockService, StockService>();

			services.AddScoped<ILogisticsService, LogisticsService>();
			services.AddScoped<ILogisticsRateService, LogisticsRateService>();

			// 註冊 Repository
			services.AddScoped<ILogisticsRepository, LogisticsRepository>();
			services.AddScoped<ILogisticsRateRepository, LogisticsRateRepository>();

			return services;
        }
    }
}
