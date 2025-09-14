using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexBackend.Core.DTOs.SUP
{
	// 調整庫存後回傳結果
	public class StockAdjustResultDto
	{
		public int SkuId { get; set; }
		public int TotalStock { get; set; }       // 調整後 SKU 總庫存
		public bool Success { get; set; }   // 是否成功執行
		public int AdjustedQty { get; set; }     // 成功調整的庫存量
		public int RemainingQty { get; set; }    // 尚未處理的庫存量（主要給退貨用）
	}
}
