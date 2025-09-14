using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexBackend.Infra.Models
{
	public partial class tHerdDBContext
	{
		// 加自己的 DbSet（讀取 AspNetRoles 用）
		public virtual DbSet<RoleRef> RolesRef { get; set; } = null!;

		// 這個 partial method 幾乎一定存在於 Power Tools 產的檔案結尾
		partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<RoleRef>(e =>
			{
				e.ToTable("AspNetRoles");
				e.HasKey(x => x.Id);
				e.Property(x => x.Id).HasMaxLength(450);
				e.Property(x => x.Name).HasMaxLength(256);
			});
		}
	}
}
