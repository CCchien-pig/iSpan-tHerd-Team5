using tHerdBackend.Core.Interfaces.SUP;
using Microsoft.Extensions.DependencyInjection;

namespace tHerdBackend.Services.SUP
{
    public static class SUPModuleServiceCollectionExtensions
    {
        public static IServiceCollection AddSUPModule(this IServiceCollection services)
        {
			// 註冊 Service
			services.AddScoped<IStockBatchService, StockBatchService>();

			services.AddScoped<IStockService, StockService>();
			
			// 註冊 Repository (如果有 Repository 層)
			//services.AddScoped<IStockBatchRepository, StockBatchRepository>();

			return services;
        }
    }
}
