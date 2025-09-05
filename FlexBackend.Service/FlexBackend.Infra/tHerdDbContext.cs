
using FlexBackend.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlexBackend.Infra
{
    public class tHerdDbContext : DbContext
    {
        public tHerdDbContext(DbContextOptions<tHerdDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
    }
}
