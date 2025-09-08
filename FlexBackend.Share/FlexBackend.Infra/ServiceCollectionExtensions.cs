using FlexBackend.Infra.DBSetting;
using FlexBackend.Infra.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlexBackend.Infra
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
            services.AddDbContext<THerdDBContext>(options => options.UseSqlServer(connectionString));

            return services;
        }
    }
}
