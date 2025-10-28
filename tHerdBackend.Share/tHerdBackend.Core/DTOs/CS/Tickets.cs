using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.DTOs.CS
{
	public sealed class TicketIn
	{
		public int? SessionId { get; set; }

		// 分類編號 (對應 CS_Ticket.CategoryId)
		public int CategoryId { get; set; }

		public int? UserId { get; set; }
		public string? ClientSessionKey { get; set; }
		public string Subject { get; set; } = "";
		public string Content { get; set; } = "";
		public byte? Priority { get; set; }  // 0=一般,1=高
	}

	public sealed class TicketOut
	{
		public int TicketId { get; set; }
		public string TicketNo { get; set; } = "";
	}

}
