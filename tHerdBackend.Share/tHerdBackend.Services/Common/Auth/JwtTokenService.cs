using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using tHerdBackend.Core.Abstractions.Security;
using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Services.Common.Auth
{
	public class JwtTokenService : IJwtTokenService
	{
		private readonly IConfiguration _config;

		// ✅ 移除 string 參數
		public JwtTokenService(IConfiguration config)
		{
			_config = config;
		}

		public (string token, DateTime expiresAtUtc, string jti) Generate(ApplicationUser user, IList<string> roles)
		{
			var jwt = _config.GetSection("Jwt");
			var signingKey = jwt["SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey not configured");
			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			// ✅ 在方法內產生 jti，不要放到建構子
			var jti = Guid.NewGuid().ToString("N");

			var claims = new List<Claim>
			{
				new("jti", jti),
				new("sub", user.Id),
				new("email", user.Email ?? string.Empty),
				new("name", $"{user.LastName}{user.FirstName}"),
				new("user_number_id", user.UserNumberId.ToString())
			};
			foreach (var r in roles) claims.Add(new Claim("role", r));

			var expiresAtUtc = DateTime.UtcNow.AddHours(2);

			var token = new JwtSecurityToken(
				issuer: jwt["Issuer"],
				audience: jwt["Audience"],
				claims: claims,
				notBefore: DateTime.UtcNow,
				expires: expiresAtUtc,
				signingCredentials: creds
			);

			var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
			return (tokenString, expiresAtUtc, jti);
		}
	}
}
