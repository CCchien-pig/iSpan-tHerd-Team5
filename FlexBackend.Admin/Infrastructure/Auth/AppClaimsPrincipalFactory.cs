// Infrastructure/Auth/AppClaimsPrincipalFactory.cs
using FlexBackend.Admin.Infrastructure.Auth;
using FlexBackend.Core.DTOs.USER;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Globalization;
using FlexBackend.Infra.Models;
using FlexBackend.Core.Abstractions;

public class AppClaimsPrincipalFactory
  : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
{
	public AppClaimsPrincipalFactory(
		UserManager<ApplicationUser> userManager,
		RoleManager<ApplicationRole> roleManager,
		IOptions<IdentityOptions> optionsAccessor)
		: base(userManager, roleManager, optionsAccessor) { }

	// ✅ 覆寫正確的簽名：回傳 ClaimsIdentity
	protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
	{
		// 這裡拿到的就是 ClaimsIdentity，不是 ClaimsPrincipal
		var identity = await base.GenerateClaimsAsync(user);

		var first = user.FirstName ?? string.Empty;
		var last = user.LastName ?? string.Empty;
		var full = $"{last} {first}".Trim();

		//（可選）避免重複：先移除舊的同類型 claims
		identity.RemoveClaim(identity.FindFirst(AppClaimTypes.UserNumberId) ?? new Claim(AppClaimTypes.UserNumberId, ""));
		identity.RemoveClaim(identity.FindFirst(AppClaimTypes.FirstName) ?? new Claim(AppClaimTypes.FirstName, ""));
		identity.RemoveClaim(identity.FindFirst(AppClaimTypes.LastName) ?? new Claim(AppClaimTypes.LastName, ""));
		identity.RemoveClaim(identity.FindFirst(AppClaimTypes.FullName) ?? new Claim(AppClaimTypes.FullName, ""));

		if (user.UserNumberId > 0)
			identity.AddClaim(new Claim(AppClaimTypes.UserNumberId,
				user.UserNumberId.ToString(CultureInfo.InvariantCulture)));

		if (!string.IsNullOrWhiteSpace(first))
			identity.AddClaim(new Claim(AppClaimTypes.FirstName, first));

		if (!string.IsNullOrWhiteSpace(last))
			identity.AddClaim(new Claim(AppClaimTypes.LastName, last));

		if (!string.IsNullOrWhiteSpace(full))
			identity.AddClaim(new Claim(AppClaimTypes.FullName, full));

		// ✅ 回傳 ClaimsIdentity
		return identity;
	}
}
