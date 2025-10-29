using Microsoft.Extensions.DependencyInjection;
using tHerdBackend.Core.Interfaces.CS;
using tHerdBackend.Infra.Repositories.CS;
using tHerdBackend.Infra.Repositories.Interfaces.CS;
using tHerdBackend.Infra.Repository.CS;
using tHerdBackend.Services.CS;


namespace tHerdBackend.Services.CS
{
    public static class CSModuleServiceCollectionExtensions
    {
        public static IServiceCollection AddCSModule(this IServiceCollection services)
        {
			// Repository（Infra）
			services.AddScoped<IFaqRepository, FaqRepository>();
			// Service
			services.AddScoped<IFaqService, FaqService>();
			services.AddScoped<ICsTicketRepository, CsTicketRepository>();
			services.AddScoped<CsTicketService>();

			return services;
        }
    }
}
