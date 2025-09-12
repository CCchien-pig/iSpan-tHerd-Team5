using System.ComponentModel.DataAnnotations;

namespace FlexBackend.SUP.Rcl.Areas.SUP.ViewModels
{
	public class StockBatchContactViewModel
	{
		public int StockBatchId { get; set; }

		public int SkuId { get; set; }

		public string SkuCode { get; set; }           // 新增 SKU 顯示

		[Display(Name = "批號")]
		public string BatchNumber { get; set; }

		[Display(Name = "數量")]
		[Required(ErrorMessage = "請填入數量")]
		public int Qty { get; set; }

		// 是否可銷售
		[Display(Name = "上下架狀態(檢視)")]
		public bool IsSellable { get; set; }

		[Display(Name = "有效日期")]
		[DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}", ApplyFormatInEditMode = true)]

		public int ShelfLifeDays { get; set; }  // 從 SKU 帶進來
		public DateTime? ExpireDate
		{
			get
			{
				if (ManufactureDate.HasValue && ShelfLifeDays > 0)
					return ManufactureDate.Value.AddDays(ShelfLifeDays);
				return null;
			}
		}

		[Display(Name = "製造日期")]
		[DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}", ApplyFormatInEditMode = true)]
		public DateTime? ManufactureDate { get; set; }

		// 唯讀欄位

		[Display(Name = "建檔人員ID")]
		public int? Creator { get; set; }

		[Display(Name = "建檔時間")]
		[DisplayFormat(DataFormatString = "{0:yyyy/MM/dd tt hh:mm:ss}", ApplyFormatInEditMode = true)]
		public DateTime CreatedDate { get; set; }

		[Display(Name = "最後異動人員ID")]
		public int? Reviser { get; set; }

		[Display(Name = "最後異動時間")]
		[DisplayFormat(DataFormatString = "{0:yyyy/MM/dd tt hh:mm:ss}", ApplyFormatInEditMode = true)]
		public DateTime? RevisedDate { get; set; }


		// 其他欄位，例如異動類型、變動量、備註等
		public string MovementType { get; set; }
		public bool IsAdd { get; set; }          // 手動調整時加/減
		public int ChangeQty { get; set; }       // 變動數量
		public string Remark { get; set; }
		public int? UserId { get; set; }
		public int SafetyStockQty { get; set; }
		public int ReorderPoint { get; set; }
		public int MaxStockQty { get; set; }
		public string ProductName { get; set; }       // 商品名稱 (唯讀)
		public string BrandName { get; set; }         // 品牌名稱 (唯讀)
		public int? BrandId { get; set; }
		public int? ProductId { get; set; }
	}	
}

