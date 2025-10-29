using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using tHerdBackend.Core.DTOs.USER;


namespace tHerdBackend.Infra.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
	{
		public DbSet<RefreshToken> RefreshTokens { get; set; } = default!;
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

			// ★ 只指定 RefreshTokens 到 auth schema（不要呼叫 EnsureSchema）
			builder.Entity<RefreshToken>(e =>
			{
				e.ToTable("RefreshTokens", "auth");  // 指定 schema
				e.HasKey(x => x.Id);
				e.Property(x => x.TokenHash).IsRequired();
				e.Property(x => x.JwtId).IsRequired();
				e.Property(x => x.UserId).IsRequired();
				e.HasIndex(x => x.UserId);

				e.HasOne(x => x.User)
				 .WithMany()                         // 不要求反向導覽，避免與業務 DbContext 耦合
				 .HasForeignKey(x => x.UserId)
				 .OnDelete(DeleteBehavior.Cascade);
			});
		}
	}
}
