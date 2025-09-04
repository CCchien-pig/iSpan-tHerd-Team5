
using FlexBackend.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlexBackend.Infra
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
    }
}
