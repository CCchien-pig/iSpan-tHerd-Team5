using FlexBackend.USER.Rcl.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace FlexBackend.USER.Rcl.Services
{
	public class RoleInitializer
	{
		public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
		{
			var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

			// List of all the roles you want to create
			string[] roleNames = { "Member", "超級管理員", "會員管理員","商品管理員", "供應鏈管理員", "活動管理員", "訂單管理員", "客服管理員", "內容管理員", };

			// Check if the "Member" role already exists
			foreach (var roleName in roleNames)
			{
				// Check if the role already exists
				if (!await roleManager.RoleExistsAsync(roleName))
				{
					var role = new ApplicationRole
					{
						Name = roleName,
						Description = $"{roleName} 的角色",
						CreatedDate = DateTime.Now,
					};

					await roleManager.CreateAsync(role); // ← 這裡要呼叫 CreateAsync
				}
			}

			// You can add logic for other roles here, like "Admin"
			//if (!await roleManager.RoleExistsAsync("Admin"))
			//{
			//	await roleManager.CreateAsync(new IdentityRole("Admin"));
			//}
		}
	}
}
