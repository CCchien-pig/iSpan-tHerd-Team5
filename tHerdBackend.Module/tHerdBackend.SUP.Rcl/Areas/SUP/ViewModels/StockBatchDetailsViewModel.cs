using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.SUP.Rcl.Areas.SUP.ViewModels
{
	public class StockBatchDetailsViewModel
	{
		public int StockBatchId { get; set; }
		public string SkuCode { get; set; }
		public string ProductName { get; set; }
		public string BrandName { get; set; }
		public string BatchNumber { get; set; }
		public DateTime? ExpireDate { get; set; }
		public List<StockHistoryViewModel> Histories { get; set; }
	}

}
