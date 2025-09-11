using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FlexBackend.USER.Rcl.Data
{
	public class UserDbContext : IdentityDbContext<IdentityUser>
	{
		public UserDbContext(DbContextOptions<UserDbContext> options)
			: base(options)
		{
		}

		// 你可以在這裡加入其他與使用者相關的 DbSet，例如會員資料、地址等
		// public DbSet<MemberProfile> MemberProfiles { get; set; }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);
			// 可以在這裡設定資料庫模型，例如設定表名、屬性等
		}
	}
}
