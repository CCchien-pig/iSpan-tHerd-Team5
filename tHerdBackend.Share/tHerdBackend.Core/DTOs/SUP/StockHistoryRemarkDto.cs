using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.DTOs.SUP
{
	public class StockHistoryRemarkDto
	{
		public int StockHistoryId { get; set; }  
		public int StockBatchId { get; set; }
		public string? Remark { get; set; }
	}

}
