using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tHerdBackend.Core.DTOs.USER;

namespace tHerdBackend.Core.Abstractions.Security
{
	public interface IJwtTokenService
	{
		(string token, DateTime expiresAtUtc, string jti) Generate(ApplicationUser user, IList<string> roles);
	}
}
