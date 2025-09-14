using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using FlexBackend.Core.DTOs.USER;

namespace FlexBackend.USER.Rcl.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
	{
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);
			builder.HasSequence<int>("UserNumberSequence", "dbo").StartsAt(1001).IncrementsBy(1);

			builder.Entity<ApplicationUser>()
				.Property(u => u.UserNumberId)
				.HasDefaultValueSql("NEXT VALUE FOR dbo.UserNumberSequence");
			// 使用 HasDefaultValue() 方法明確設定 IsActive 欄位的預設值
			builder.Entity<ApplicationUser>()
				   .Property(u => u.IsActive)
				   .HasDefaultValue(true);
		}
	}
}
