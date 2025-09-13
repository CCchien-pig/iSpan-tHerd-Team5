using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FlexBackend.Infra.Models;

public partial class tHerdDBContext : DbContext
{
    public tHerdDBContext()
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            IConfiguration config = new ConfigurationBuilder()
                 .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                 .AddJsonFile("appsettings.json", optional: true)
                 .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                 .AddUserSecrets<tHerdDBContext>(optional: true)
                 .Build();

            var cs = config.GetConnectionString("DefaultConnection")
                     ?? config["ConnectionStrings:DefaultConnection"];

            if (!string.IsNullOrWhiteSpace(cs))
                optionsBuilder.UseSqlServer(cs);
        }
    }
}