namespace tHerdBackend.Core.DTOs.SUP.Stock
{
	public class StockBatchUpdateDto
	{
		public int StockBatchId { get; set; }   // 要異動的批號ID

		public int ChangeQty { get; set; }      // 異動數量（正數=增加，負數=減少）

		public bool IsAdd { get; set; }         // true=增加, false=減少

		public string Remark { get; set; }      // 備註

		public string UserId { get; set; }      // 操作人（可選，看你要不要記錄）
	}

}