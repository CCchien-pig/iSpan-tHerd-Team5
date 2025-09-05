using FlexBackend.Core.Interfaces.SYS;
using FlexBackend.Infra.DBSetting;
using FlexBackend.Infra.Repository.SYS;
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
            services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(connectionString));

            // 把介面對應到實作
            services.AddScoped<ISysProgramConfigRepository, SysProgramConfigRepository>();

            return services;
        }
    }
}
