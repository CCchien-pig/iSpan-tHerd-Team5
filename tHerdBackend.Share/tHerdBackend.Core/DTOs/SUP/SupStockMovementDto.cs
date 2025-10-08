namespace tHerdBackend.Core.DTOs.SUP
{
	/// <summary>
	/// 用於每筆異動紀錄
	/// 包含 SKU、批次、異動類型、數量、前後庫存、操作人、顯示資訊等
	/// </summary>
	public class SupStockMovementDto
	{
		public int SkuId { get; set; }					// 影響哪個 SKU
		public int StockBatchId { get; set; }			// 批次ID 
		public string MovementType { get; set; }		// Purchase / Sale / Return / Expire / Adjust
		public int ChangeQty { get; set; }				// 異動數量
		public bool IsAdd { get; set; }					// Adjust 用，判斷是加還是減
		
		public int CurrentQty { get; set; }             // 異動前庫存數量
		public int AfterQty { get; set; }               // 異動後庫存數量 (方便查詢)
		
		public bool IsSellable { get; set; }			// 是否可售
		public int? UserId { get; set; }				// 操作者
		public string? Remark { get; set; }				// 備註

		public int? OrderId { get; set; }               // 如果是由訂單觸發，記錄訂單ID
		public int? OrderDetailId { get; set; }         // 方便追蹤到哪個明細
		public string? ReferenceNo { get; set; }        // 外部單號(採購單/銷貨單/退貨單...)
		public DateTime MovementDate { get; set; }      // 異動時間

		// 顯示用欄位
		public string? SkuCode { get; set; }
		public string? ProductName { get; set; }
		public string? BrandName { get; set; }
		public string? BatchNumber { get; set; }
		public int PredictedQty { get; set; }          // 前端預計庫存顯示
	}
}
	// [SYS_Code]
	// ModuleId CodeId  CodeNo CodeDesc
	// ('SUP', '00', '01', N'庫存異動類型'),
	// ('SUP', '01', 'Purchase', N'採購入庫'),
	// ('SUP', '01', 'Sale', N'銷售出庫'),
	// ('SUP', '01', 'Return', N'退貨入庫'),
	// ('SUP', '01', 'Expire', N'到期報廢'),
	// ('SUP', '01', 'Adjust', N'手動調整'),
