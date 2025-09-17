namespace FlexBackend.SUP.Rcl.Areas.SUP.Controllers
{
	public class StockBatchEditDto
	{
		public int StockBatchId { get; set; }

		// 唯讀顯示欄位
		public string BrandName { get; set; } = string.Empty;
		public string ProductName { get; set; } = string.Empty;
		public string SkuCode { get; set; } = string.Empty;
		public DateTime? ManufactureDate { get; set; }

		// 異動欄位
		public int CurrentQty { get; set; }        // 異動前庫存
		public bool IsAdd { get; set; } = true;    // + / - 選擇
		public int ChangeQty { get; set; } = 0;    // 異動數量
		public string Remark { get; set; } = string.Empty;  // 備註
	}

}