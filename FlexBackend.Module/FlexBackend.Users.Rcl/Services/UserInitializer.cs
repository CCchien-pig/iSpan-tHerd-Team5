using FlexBackend.USER.Rcl.Data;
using FlexBackend.USER.Rcl.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq; // 新增這個引用
using System.Threading.Tasks;
namespace FlexBackend.USER.Rcl.Services
{
	public static class UserInitializer
	{
		// 定義一些台灣常見的姓氏和名字
		private static readonly string[] LastNames = { "陳", "林", "黃", "張", "李", "王", "吳", "劉", "蔡", "楊" };
		private static readonly string[] FirstNames = { "志明", "建宏", "俊傑", "家豪", "冠宇", "雅雯", "欣慧", "淑芬", "佩蓉", "美玲" };

		public static async Task SeedUsersAsync(IServiceProvider serviceProvider)
		{
			var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
			var configuration = serviceProvider.GetRequiredService<IConfiguration>();

			var superAdminPassword = configuration["SuperAdminPassword"];
			var moduleAdminPassword = configuration["ModuleAdminPassword"];

			if (string.IsNullOrEmpty(superAdminPassword) || string.IsNullOrEmpty(moduleAdminPassword))
			{
				throw new InvalidOperationException("Admin passwords are not configured in secrets.json.");
			}

			string[] moduleAdminRoles = {
			"會員管理員", "商品管理員", "供應鏈管理員",
			"活動管理員", "訂單管理員", "客服管理員",
			"內容管理員"
		};

			var rand = new Random();

			// 1. 創建超級管理員
			var superAdminUser = new ApplicationUser
			{
				UserName = "superadmin@ispan.com",
				Email = "superadmin@ispan.com",
				EmailConfirmed = true,
				LastName = "超級",
				FirstName = "管理員",
				Gender = "男",
				IsActive = true,
			};
			if (await userManager.FindByEmailAsync(superAdminUser.Email) == null)
			{
				var result = await userManager.CreateAsync(superAdminUser, superAdminPassword);
				if (result.Succeeded) await userManager.AddToRoleAsync(superAdminUser, "超級管理員");
			}

			// 2. 創建 7 位模組管理員
			for (int i = 0; i < moduleAdminRoles.Length; i++)
			{
				var roleName = moduleAdminRoles[i];
				var email = $"modadmin{i + 1}@ispan.com";

				var moduleAdminUser = new ApplicationUser
				{
					UserName = email,
					Email = email,
					EmailConfirmed = true,
					LastName = LastNames[rand.Next(LastNames.Length)],
					FirstName = FirstNames[rand.Next(FirstNames.Length)],
					Gender = "男",
					IsActive = true,
				};
				if (await userManager.FindByEmailAsync(moduleAdminUser.Email) == null)
				{
					var result = await userManager.CreateAsync(moduleAdminUser, moduleAdminPassword);
					if (result.Succeeded) await userManager.AddToRoleAsync(moduleAdminUser, roleName);
				}
			}

			// 3. 創建其餘 42 位普通會員
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
					MemberRankId = "MR001"
				};

				if (await userManager.FindByEmailAsync(user.Email) == null)
				{
					var result = await userManager.CreateAsync(user, "User-Strong-Password");
					if (result.Succeeded) await userManager.AddToRoleAsync(user, "Member");
				}
			}
		}
	}
}
