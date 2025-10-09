using tHerdBackend.Core.DTOs.USER;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.USER.Rcl.Services
{
	public class UserService
	{
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly ApplicationDbContext _context;

		public UserService(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
		{
			_userManager = userManager;
			_context = context;
		}

		// 負責建立新使用者並自動產生推薦碼
		public async Task<IdentityResult> RegisterUserAsync(string email, string password, string firstName, string lastName,string gender)
		{

			var newUser = new ApplicationUser
			{
				UserName = email,
				Email = email,
				FirstName = firstName,
				LastName = lastName,
				Gender = gender,
				//UserNumberId = nextId+1000
			};

			// 在儲存前，呼叫方法來產生並賦值 ReferralCode
			newUser.ReferralCode = GenerateReferralCode();

			var result = await _userManager.CreateAsync(newUser, password);

			// 如果建立成功，通常你會在這裡賦予角色
			if (result.Succeeded)
			{
				// 例如：賦予 Member 角色
				await _userManager.AddToRoleAsync(newUser, "Member");
			}

			return result;
		}

		// 產生推薦碼的私有方法
		public string GenerateReferralCode()
		{
			// 這裡可以實現你想要的任何邏輯
			// 範例：使用 GUID 的前八碼作為唯一識別碼
			var uniquePart = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8);
			return $"REF-{uniquePart.ToUpper()}";
		}
	}
}
