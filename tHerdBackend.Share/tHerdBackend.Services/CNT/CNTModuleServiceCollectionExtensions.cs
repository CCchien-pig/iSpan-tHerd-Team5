using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using tHerdBackend.Core.Interfaces.CNT;
using tHerdBackend.Core.Interfaces.Nutrition;
using tHerdBackend.Infra.Repository.CNT;
using tHerdBackend.Services.Payments;
using Microsoft.Extensions.Configuration;

namespace tHerdBackend.Services.CNT
{
	public static class CNTModuleServiceCollectionExtensions
	{
		public static IServiceCollection AddCNTModule(this IServiceCollection services, IConfiguration configuration)
		{
			// 🟢 正確註冊 CNT 模組服務
			services.AddScoped<IContentService, ContentService>();
			services.AddScoped<ICntQueryRepository, CntQueryRepository>();
			services.AddScoped<INutritionService, NutritionService>();
			services.AddScoped<INutritionRepository, NutritionRepository>();
			services.AddScoped<IProductTagRepository, ProductBriefRepository>();
			services.AddScoped<IContentProductService, ContentProductService>();
			services.AddScoped<ITagProductReadRepository, TagProductReadRepository>();
			services.AddScoped<ITagProductQueryService, TagProductQueryService>();
			services.AddScoped<ICntPurchaseRepository, CntPurchaseRepository>();
			services.AddScoped<ICntPurchaseService, CntPurchaseService>();
			// 1) 讀取 appsettings 的 LinePay 區段
			services.Configure<LinePayOptions>(configuration.GetSection("LinePay"));

			// 2) 註冊 HttpClient + LinePayClient
			services.AddHttpClient<LinePayClient>();

			return services;
		}
	}
}
