using Microsoft.Extensions.DependencyInjection;
using tHerdBackend.Core.Interfaces.MKT;
using tHerdBackend.Core.Interfaces.Repositories.MKT;
using tHerdBackend.Infra.Services.MKT;
using tHerdBackend.Infrastructure.Repositories.MKT;
using tHerdBackend.Infrastructure.Services.MKT;

namespace tHerdBackend.Services.MKT
{
    public static class MKTModuleServiceCollectionExtensions
    {
        public static IServiceCollection AddMKTModule(this IServiceCollection services)
        {
            services.AddScoped<ICampaignService, CampaignService>();
            services.AddScoped<ICampaignRepository, CampaignRepository>();

            // 🚀 加在這裡 (ConfigureServices 位置)
            services.AddScoped<IMktCouponService, MktCouponService>();

            return services;
        }

    }
}
