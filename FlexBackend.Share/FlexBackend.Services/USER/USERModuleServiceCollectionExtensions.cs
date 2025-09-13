using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
namespace FlexBackend.Services.USER
{
    public static class USERModuleServiceCollectionExtensions
    {
        public static IServiceCollection AddUSERDModule(this IServiceCollection services)
        {
            return services;
        }
    }
}
