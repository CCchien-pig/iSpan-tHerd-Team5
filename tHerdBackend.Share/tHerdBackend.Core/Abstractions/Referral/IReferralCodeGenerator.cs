using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.Abstractions.Referral
{
	public interface IReferralCodeGenerator
	{
		string Generate();
	}
}
