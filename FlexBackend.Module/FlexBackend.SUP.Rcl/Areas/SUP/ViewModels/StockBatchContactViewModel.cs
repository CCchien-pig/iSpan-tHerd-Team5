using System.ComponentModel.DataAnnotations;

namespace FlexBackend.SUP.Rcl.Areas.SUP.ViewModels
{
	public class StockBatchContactViewModel : IValidatableObject
	{
		public int StockBatchId { get; set; }

		// SKU 基本資訊
		public int SkuId { get; set; }
		public string? SkuCode { get; set; }

		[Display(Name = "批號")]
		public string? BatchNumber { get; set; }

		[Display(Name = "上下架狀態(檢視)，別用這個，直接用[ProdProduct]的[IsPublished]")]
		public bool IsSellable { get; set; }

		// 系統資訊
		public int? UserId { get; set; }

		[Display(Name = "建檔人員ID")]
		public int? Creator { get; set; }

		[Display(Name = "建檔時間")]
		public DateTime CreatedDate { get; set; }

		[Display(Name = "最後異動人員ID")]
		public int? Reviser { get; set; }

		[Display(Name = "最後異動時間")]
		public DateTime? RevisedDate { get; set; }


		// 異動數量，Create/Edit 使用
		[Display(Name = "異動數量")]
		public int? Qty { get; set; }  // 改成可空，由 ChangeQty 控制

		[Display(Name = "異動類型")]
		public string? MovementType { get; set; }

		[Display(Name = "手動調整數量是否為正數")]
		public bool IsAdd { get; set; }

		[Display(Name = "異動數量")]
		public int? ChangeQty { get; set; }  // 改成可空，由驗證控制

		[Display(Name = "異動備註")]
		public string? Remark { get; set; }


		// 商品 / 品牌資訊
		public int? BrandId { get; set; }
		public string? BrandName { get; set; }

		[Display(Name = "商品ID")]
		public int? ProductId { get; set; }   // [ProdProduct]的[ProductId]	

		[Display(Name = "商品名稱")]
		public string? ProductName { get; set; }   // [ProdProduct]的[ProductName]	

		[Display(Name = "是否上架（0=否，1=是）")]
		public bool IsPublished { get; set; }   // [ProdProduct]的[IsPublished]	


		// 庫存資訊
		[Display(Name = "安全庫存量")]
		public int SafetyStockQty { get; set; }    // [ProdProductSku]的[StockQty]

		[Display(Name = "再訂購點")]
		public int ReorderPoint { get; set; }    // [ProdProductSku]的[ReorderPoint]

		[Display(Name = "最大庫存量（0=不限制)")]
		public int MaxStockQty { get; set; }    // [ProdProductSku]的[MaxStockQty]	

		[Display(Name = "當前庫存")]
		public int CurrentQty { get; set; } = 0;    // [ProdProductSku]的[StockQty]

		[Display(Name = "預計庫存")]
		public int PredictedQty { get; set; } = 0;

		[Display(Name = "是否可缺貨預購")]
		public bool IsAllowBackorder { get; set; } = false; // [ProdProductSku]的[IsAllowBackorder]

		[Display(Name = "有效天數（0=無限制）")]
		public int ShelfLifeDays { get; set; }  // [ProdProductSku]的[ShelfLifeDays]


		[Display(Name = "製造日期")]
		[Required(ErrorMessage = "請選擇製造日期")]
		[DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}", ApplyFormatInEditMode = true)]
		public DateTime? ManufactureDate { get; set; }

		// 自選製造日期+有效天數=到期日
		[Display(Name = "到期日")]
		[DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}", ApplyFormatInEditMode = true)]
		public DateTime? ExpireDate
		{
			get
			{
				if (ManufactureDate.HasValue && ShelfLifeDays > 0)
					return ManufactureDate.Value.AddDays(ShelfLifeDays);
				return null;
			}
		}

		// 自訂驗證邏輯
		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (!string.IsNullOrEmpty(MovementType) && (!ChangeQty.HasValue || ChangeQty.Value <= 0))
			{
				yield return new ValidationResult(
					"異動類型時必須輸入大於 0 的異動數量",
					new[] { nameof(ChangeQty) });
			}
		}


		// 提供前端選單用
		public List<SkuOption> SkuOptions { get; set; } = new();
	}

	// 前端下拉選單用的 DTO
	public class SkuOption
	{
		public int SkuId { get; set; }
		public string SkuCode { get; set; } = "";
	}

}
