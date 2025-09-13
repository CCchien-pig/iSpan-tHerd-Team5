using FlexBackend.Core.Interfaces.PROD;
using FlexBackend.Core.Interfaces.Products;
using FlexBackend.Core.Interfaces.SUP;
using FlexBackend.Infra.Repository.PROD;
using FlexBackend.Services.PROD;
using Microsoft.Extensions.DependencyInjection;

namespace FlexBackend.Services.SUP
{
    public static class SUPModuleServiceCollectionExtensions
    {
        public static IServiceCollection AddSUPModule(this IServiceCollection services)
        {
			// 註冊 Service
			services.AddScoped<IStockBatchService, StockBatchService>();

			// 註冊 Repository (如果你也有 Repository 層)
			//services.AddScoped<IStockBatchRepository, StockBatchRepository>();

			return services;
        }
    }
}
