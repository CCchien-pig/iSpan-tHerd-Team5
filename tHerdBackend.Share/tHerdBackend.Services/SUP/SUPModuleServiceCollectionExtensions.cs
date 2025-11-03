using Microsoft.Extensions.DependencyInjection;
using tHerdBackend.Core.DTOs.SUP.Brand;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Core.Interfaces.SYS;
using tHerdBackend.Core.Services.SUP;
using tHerdBackend.Infra.Repository.SUP;
using tHerdBackend.Services.Common.SYS;

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
			services.AddScoped<IBrandLayoutService, BrandLayoutService>();
			services.AddScoped<IBrandLogoService, BrandLogoService>();


			// 註冊 Repository
			services.AddScoped<ISupplierRepository, SupplierRepository>();

			services.AddScoped<ILogisticsRepository, LogisticsRepository>();
			services.AddScoped<ILogisticsRateRepository, LogisticsRateRepository>();

			services.AddScoped<IShippingFeeRepository, ShippingFeeRepository>();
			
			services.AddScoped<IBrandRepository, BrandRepository>();
			services.AddScoped<IBrandLayoutRepository, BrandLayoutRepository>();
			services.AddScoped<IBrandAssetFileRepository, BrandAssetFileRepository>();


			// 1. 註冊通用的內容服務 (用於 Controller 內部的協調)
			services.AddScoped<IContentService, ContentService>();

			// 2. 註冊所有內容模組的泛型倉儲 (這是關鍵修正，用泛型介面註冊實作)
			services.AddScoped<IGenericContentRepository<BrandAccordionContentDto>, BrandAccordionRepository>();
			services.AddScoped<IGenericContentRepository<BannerDto>, BrandBannerRepository>();
			services.AddScoped<IGenericContentRepository<BrandArticleDto>, BrandArticleRepository>();

			services.AddScoped<ISysAssetFileService, SysAssetFileService>();

			services.AddHttpClient();

			return services;
        }
    }
}
