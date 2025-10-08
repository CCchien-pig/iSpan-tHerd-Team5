using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
namespace tHerdBackend.Services.USER
{
    public static class USERModuleServiceCollectionExtensions
    {
        public static IServiceCollection AddUSERDModule(this IServiceCollection services)
        {
            return services;
        }
    }
}
