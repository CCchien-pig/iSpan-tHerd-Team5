using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.DTOs.CS
{
	namespace tHerdBackend.Core.Entities.CS
	{
		public class Tickets
		{
			public int TicketId { get; set; }
			public int? UserId { get; set; }
			public int? CategoryId { get; set; }
			public string Subject { get; set; } = "";
			public int Status { get; set; } = 0; // 0=待處理,1=處理中,2=已完成,3=已關閉
			public int Priority { get; set; } = 2; // 1=高,2=中,3=低
			public DateTime CreatedDate { get; set; } = DateTime.Now;
			public DateTime? RevisedDate { get; set; }
		}
	}


}
