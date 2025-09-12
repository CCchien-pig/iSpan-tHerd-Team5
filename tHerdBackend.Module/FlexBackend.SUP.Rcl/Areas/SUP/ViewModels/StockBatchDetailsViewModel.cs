using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexBackend.SUP.Rcl.Areas.SUP.ViewModels
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

	public class StockHistoryViewModel
	{
		public int StockHistoryId { get; set; }
		public string ChangeType { get; set; }
		public int ChangeQty { get; set; }
		public int BeforeQty { get; set; }
		public int AfterQty { get; set; }
		public string Remark { get; set; }
		public int? Reviser { get; set; }
		public DateTime RevisedDate { get; set; }
	}


}
