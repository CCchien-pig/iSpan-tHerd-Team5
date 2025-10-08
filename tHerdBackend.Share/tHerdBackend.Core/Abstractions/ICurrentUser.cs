using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.Abstractions
{
	public interface ICurrentUser
	{
		bool IsAuthenticated { get; }
		string Id { get; }
		int UserNumberId { get; }
		string? Email { get; }
		string? FullName { get; }
	}
}
