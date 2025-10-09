using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.USER;

namespace tHerdBackend.USER.Rcl.Services
{
	public static class UserInitializer
	{
		// 台灣常見姓氏與名字
		private static readonly string[] LastNames = { "陳", "林", "黃", "張", "李", "王", "吳", "劉", "蔡", "楊" };
		private static readonly string[] FirstNames = { "志明", "建宏", "俊傑", "家豪", "冠宇", "雅雯", "欣慧", "淑芬", "佩蓉", "美玲" };

		public static async Task SeedUsersAsync(IServiceProvider serviceProvider)
		{
			var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
			var configuration = serviceProvider.GetRequiredService<IConfiguration>();
			var userService = serviceProvider.GetRequiredService<UserService>();

			// ★ 靜態類別不能用 ILogger<T>，改用 ILoggerFactory 建立具名 logger
			var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
			ILogger logger = loggerFactory.CreateLogger(nameof(UserInitializer));

			// 讀取三組密碼
			var superAdminPassword = configuration["SuperAdminPassword"];
			var moduleAdminPassword = configuration["ModuleAdminPassword"];
			var userPassword = configuration["UserPassword"];

			// 三組密碼都必須存在
			if (string.IsNullOrWhiteSpace(superAdminPassword))
				throw new InvalidOperationException("SuperAdminPassword not configured in secrets.json.");
			if (string.IsNullOrWhiteSpace(moduleAdminPassword))
				throw new InvalidOperationException("ModuleAdminPassword not configured in secrets.json.");
			if (string.IsNullOrWhiteSpace(userPassword))
				throw new InvalidOperationException("UserPassword not configured in secrets.json.");

			// 若已存在任一使用者，避免重複 seeding（可依需求調整）
			//if (await userManager.Users.AnyAsync())
			//{
			//	logger.LogInformation("Users already exist. Skipping user seeding.");
			//	return;
			//}

			// 模組管理員角色清單（需先由 RoleInitializer 建好）
			string[] moduleAdminRoles =
			{
				"會員管理員", "商品管理員", "供應鏈管理員",
				"活動管理員", "訂單管理員", "客服管理員",
				"內容管理員"
			};

			var rand = new Random();

			// 1) 超級管理員
			var super = new ApplicationUser
			{
				UserName = "superadmin@ispan.com",
				Email = "superadmin@ispan.com",
				EmailConfirmed = true,
				LastName = "超級",
				FirstName = "管理員",
				Gender = "男",
				IsActive = true,
				MemberRankId = "MR001"
			};

			if (await userManager.FindByEmailAsync(super.Email) == null)
			{
				var rCreate = await userManager.CreateAsync(super, superAdminPassword);
				if (!rCreate.Succeeded)
				{
					var msg = "Create super admin failed: " + string.Join(", ", rCreate.Errors.Select(e => $"{e.Code}:{e.Description}"));
					logger.LogError(msg);
					throw new InvalidOperationException(msg);
				}

				var rRole = await userManager.AddToRoleAsync(super, "超級管理員");
				if (!rRole.Succeeded)
				{
					var msg = "Add super admin role failed: " + string.Join(", ", rRole.Errors.Select(e => $"{e.Code}:{e.Description}"));
					logger.LogError(msg);
					throw new InvalidOperationException(msg);
				}

				logger.LogInformation("Super admin user created and added to role '超級管理員'.");
			}
			else
			{
				logger.LogInformation("Super admin already exists. Skipped creation.");
			}

			// 2) 七位模組管理員
			for (int i = 0; i < moduleAdminRoles.Length; i++)
			{
				var roleName = moduleAdminRoles[i];
				var email = $"modadmin{i + 1}@ispan.com";

				var mod = new ApplicationUser
				{
					UserName = email,
					Email = email,
					EmailConfirmed = true,
					LastName = LastNames[rand.Next(LastNames.Length)],
					FirstName = FirstNames[rand.Next(FirstNames.Length)],
					Gender = "女",
					IsActive = true,
					MemberRankId = "MR001"
				};

				if (await userManager.FindByEmailAsync(mod.Email) == null)
				{
					var rCreate = await userManager.CreateAsync(mod, moduleAdminPassword);
					if (!rCreate.Succeeded)
					{
						var msg = $"Create module admin [{email}] failed: " + string.Join(", ", rCreate.Errors.Select(e => $"{e.Code}:{e.Description}"));
						logger.LogError(msg);
						throw new InvalidOperationException(msg);
					}

					var rRole = await userManager.AddToRoleAsync(mod, roleName);
					if (!rRole.Succeeded)
					{
						var msg = $"Add role [{roleName}] to [{email}] failed: " + string.Join(", ", rRole.Errors.Select(e => $"{e.Code}:{e.Description}"));
						logger.LogError(msg);
						throw new InvalidOperationException(msg);
					}

					logger.LogInformation("Module admin {Email} created and added to role '{Role}'.", email, roleName);
				}
				else
				{
					logger.LogInformation("Module admin {Email} already exists. Skipped creation.", email);
				}
			}

			// 3) 其餘 42 位普通會員
			for (int i = 0; i < 42; i++)
			{
				var email = $"user{i + 1}@ispan.com";

				var user = new ApplicationUser
				{
					UserName = email,
					Email = email,
					EmailConfirmed = true,
					LastName = LastNames[rand.Next(LastNames.Length)],
					FirstName = FirstNames[rand.Next(FirstNames.Length)],
					Gender = (rand.Next(2) == 0) ? "男" : "女",
					IsActive = true,
					MemberRankId = "MR001",
					ReferralCode = userService.GenerateReferralCode()
				};

				if (await userManager.FindByEmailAsync(user.Email) == null)
				{
					var rCreate = await userManager.CreateAsync(user, userPassword);
					if (!rCreate.Succeeded)
					{
						var msg = $"Create user [{email}] failed: " + string.Join(", ", rCreate.Errors.Select(e => $"{e.Code}:{e.Description}"));
						logger.LogError(msg);
						throw new InvalidOperationException(msg);
					}

					var rRole = await userManager.AddToRoleAsync(user, "Member");
					if (!rRole.Succeeded)
					{
						var msg = $"Add role [Member] to [{email}] failed: " + string.Join(", ", rRole.Errors.Select(e => $"{e.Code}:{e.Description}"));
						logger.LogError(msg);
						throw new InvalidOperationException(msg);
					}

					logger.LogInformation("User {Email} created and added to role 'Member'.", email);
				}
				else
				{
					logger.LogInformation("User {Email} already exists. Skipped creation.", email);
				}
			}

			logger.LogInformation("User seeding completed successfully.");
		}
	}
}
