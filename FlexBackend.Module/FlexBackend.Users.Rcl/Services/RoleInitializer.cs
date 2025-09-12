using FlexBackend.USER.Rcl.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlexBackend.USER.Rcl.Services
{
	public class RoleInitializer
	{
		public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
		{
			var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
			var logger = serviceProvider.GetRequiredService<ILogger<RoleInitializer>>();

			string[] roleNames = { "Member", "超級管理員", "會員管理員", "商品管理員", "供應鏈管理員", "活動管理員", "訂單管理員", "客服管理員", "內容管理員" };

			foreach (var roleName in roleNames)
			{
				if (!await roleManager.RoleExistsAsync(roleName))
				{
					var role = new ApplicationRole
					{
						Name = roleName,
						Description = $"{roleName} 的角色",
						CreatedDate = DateTime.Now,
					};
					var r = await roleManager.CreateAsync(role);
					if (!r.Succeeded)
					{
						var msg = $"Create role [{roleName}] failed: " + string.Join(", ", r.Errors.Select(e => $"{e.Code}:{e.Description}"));
						logger.LogError(msg);
						throw new InvalidOperationException(msg);
					}
				}
			}
		}
	}
}