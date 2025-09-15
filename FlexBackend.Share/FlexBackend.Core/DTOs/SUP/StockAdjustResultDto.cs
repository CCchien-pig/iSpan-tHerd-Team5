using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexBackend.Core.DTOs.SUP
{
	/// <summary>
	/// 調整庫存後回傳結果
	/// 包含總庫存、成功與否、調整量、剩餘量、前端預計庫存、訊息。
	/// </summary>
	public class StockAdjustResultDto
	{
		public StockAdjustResultDto()
		{
			BatchMovements = new List<SupStockMovementDto>();
		}

		public int SkuId { get; set; }               // 對應 SKU ID
		public int TotalStock { get; set; }          // 調整後 SKU 總庫存
		public bool Success { get; set; }            // 是否成功執行
		public int AdjustedQty { get; set; }         // 成功調整的庫存量（可正可負）
		public int RemainingQty { get; set; }        // 尚未處理的庫存量（主要給退貨用）
		public int PredictedQty { get; set; }        // 前端預計庫存，考慮增加/減少及是否允許預購
		public string Message { get; set; }          // 操作訊息
		public List<SupStockMovementDto> BatchMovements { get; set; } = new();  // 每個批次扣庫明細
	}
}
