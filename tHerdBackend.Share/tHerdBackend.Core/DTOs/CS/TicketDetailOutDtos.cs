using System;
using System.Collections.Generic;

namespace tHerdBackend.Core.DTOs.CS
{
	public class TicketDetailOut
	{
		public int TicketId { get; set; }
		public string Subject { get; set; } = "";
		public string? CategoryName { get; set; }
		public string StatusText { get; set; } = "";
		public DateTime CreatedDate { get; set; }
		public List<TicketMessageDto> Messages { get; set; } = new();
	}

	

	public class TicketDto
	{
		public int TicketId { get; set; }
		public string Subject { get; set; } = "";
		public string? CategoryName { get; set; }
		public string StatusText { get; set; } = "";
		public string PriorityText { get; set; } = "";
		public DateTime CreatedDate { get; set; }

		// 寄信用欄位
		public string? Email { get; set; }
		public string? UserName { get; set; }
	}
}
