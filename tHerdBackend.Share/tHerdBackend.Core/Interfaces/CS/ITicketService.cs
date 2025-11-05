using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tHerdBackend.Core.DTOs.CS;

namespace tHerdBackend.Core.Interfaces.CS
{
	public interface ITicketService
	{
		Task<TicketOut> CreateAsync(TicketIn dto);
	}

}
