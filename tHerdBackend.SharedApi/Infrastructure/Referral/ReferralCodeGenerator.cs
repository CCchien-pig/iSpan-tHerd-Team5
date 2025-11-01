using System;
using tHerdBackend.Core.Abstractions.Referral;

namespace tHerdBackend.SharedApi.Infrastructure.Referral
{
	public class ReferralCodeGenerator : IReferralCodeGenerator
	{
		public string Generate()
		{
			var uniquePart = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8);
			return $"REF-{uniquePart.ToUpper()}";
		}
	}
}
