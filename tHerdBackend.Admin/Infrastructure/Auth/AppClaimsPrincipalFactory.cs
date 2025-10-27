// Infrastructure/Auth/AppClaimsPrincipalFactory.cs
using tHerdBackend.Admin.Infrastructure.Auth;
using tHerdBackend.Core.DTOs.USER;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Globalization;
using tHerdBackend.Infra.Models;
using tHerdBackend.Core.Abstractions;

public class AppClaimsPrincipalFactory
  : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
{
	public AppClaimsPrincipalFactory(
		UserManager<ApplicationUser> userManager,
		RoleManager<ApplicationRole> roleManager,
		IOptions<IdentityOptions> optionsAccessor)
		: base(userManager, roleManager, optionsAccessor) { }

	protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
	{
		var identity = await base.GenerateClaimsAsync(user);

		var first = user.FirstName ?? string.Empty;
		var last = user.LastName ?? string.Empty;
		var full = $"{last} {first}".Trim();

		if (user.UserNumberId > 0) // 你說一定會配號 → 也可以直接發
			identity.AddClaim(new Claim(AppClaimTypes.UserNumberId, user.UserNumberId.ToString(CultureInfo.InvariantCulture)));
		if (!string.IsNullOrWhiteSpace(first)) identity.AddClaim(new Claim(AppClaimTypes.FirstName, first));
		if (!string.IsNullOrWhiteSpace(last)) identity.AddClaim(new Claim(AppClaimTypes.LastName, last));
		if (!string.IsNullOrWhiteSpace(full)) identity.AddClaim(new Claim(AppClaimTypes.FullName, full));

		return identity;

		//    如果你很想在 Cookie 裡也擁有 'sub' 或 'name'，可以「加上去」，但不要移除既有的：
		// identity.AddClaim(new Claim("sub", user.Id));
		// identity.AddClaim(new Claim("name", full));
		
		//var id = await base.GenerateClaimsAsync(user);

		//id.RemoveClaim(id.FindFirst(ClaimTypes.NameIdentifier)!); // 移除既有 SID 型別，改用 sub
		//id.AddClaim(new Claim("sub", user.Id));
		//if (!string.IsNullOrEmpty(user.Email)) id.AddClaim(new Claim("email", user.Email));
		//id.AddClaim(new Claim("name", $"{user.LastName}{user.FirstName}"));
		//id.AddClaim(new Claim("user_number_id", user.UserNumberId.ToString()));

		//// 角色請確保用 "role"
		//var roles = await UserManager.GetRolesAsync(user);
		//foreach (var r in roles) id.AddClaim(new Claim("role", r));

		//return id;
	}
}
