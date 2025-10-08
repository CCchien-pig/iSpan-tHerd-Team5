using tHerdBackend.Infra.DBSetting;
using tHerdBackend.Infra.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace tHerdBackend.Infra
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 由 Host 專案把連線字串傳進來
        /// </summary>
        public static IServiceCollection AddFlexInfra(this IServiceCollection services, string connectionString)
        {
            // Dapper / ADO.NET
            services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(connectionString));

            // EF Core
            services.AddDbContext<tHerdDBContext>(options => options.UseSqlServer(connectionString));

            MapsterConfig.Register();

            return services;
        }
    }
}
