using Microsoft.Extensions.DependencyInjection;
using tHerdBackend.Core.Interfaces.MKT;
using tHerdBackend.Core.Interfaces.Repositories.MKT;
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
            return services;
        }

    }
}
