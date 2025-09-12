using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FlexBackend.Core.DTOs.SUP
{
	public class SupStockMovementDto
	{
		public int SkuId { get; set; }             // 影響哪個 SKU
		public int StockBatchId { get; set; }      // 批次ID 
		public string MovementType { get; set; }   // Purchase / Sale / Return / Expire / Adjust
		public int ChangeQty { get; set; }         // 異動數量
		public bool IsAdd { get; set; }            // Adjust 用，判斷是加還是減
		public int CurrentQty { get; set; }        // 異動前庫存
		public bool IsSellable { get; set; }       // 是否可售
		public int? UserId { get; set; }           // 操作者
		public string? Remark { get; set; }        // 備註
	}

	// [SYS_Code]
	// ModuleId CodeId  CodeNo CodeDesc
	// ('SUP', '00', '01', N'庫存異動類型'),
	// ('SUP', '01', 'Purchase', N'採購入庫'),
	// ('SUP', '01', 'Sale', N'銷售出庫'),
	// ('SUP', '01', 'Return', N'退貨入庫'),
	// ('SUP', '01', 'Expire', N'到期報廢'),
	// ('SUP', '01', 'Adjust', N'手動調整'),

}
