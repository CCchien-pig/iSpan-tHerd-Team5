using Microsoft.Extensions.DependencyInjection;
using tHerdBackend.Core.Interfaces.CNT;
using tHerdBackend.Core.Interfaces.Nutrition;
using tHerdBackend.Infra.Repository.CNT;

namespace tHerdBackend.Services.CNT
{
	public static class CNTModuleServiceCollectionExtensions
	{
		public static IServiceCollection AddCNTModule(this IServiceCollection services)
		{
			// 🟢 正確註冊 CNT 模組服務
			services.AddScoped<IContentService, ContentService>();
			services.AddScoped<ICntQueryRepository, CntQueryRepository>();
			services.AddScoped<INutritionService, NutritionService>();
			services.AddScoped<INutritionRepository, NutritionRepository>();

			return services;
		}
	}
}
