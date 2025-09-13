using System.ComponentModel.DataAnnotations;

namespace FlexBackend.SUP.Rcl.Areas.SUP.ViewModels
{
	public class StockBatchContactViewModel : IValidatableObject
	{
		public int StockBatchId { get; set; }
		public int SkuId { get; set; }
		public string? SkuCode { get; set; }

		[Display(Name = "批號")]
		public string? BatchNumber { get; set; }

		[Display(Name = "異動數量")]
		public int? Qty { get; set; }  // 改成可空，由 ChangeQty 控制

		[Display(Name = "上下架狀態(檢視)，別用這個，直接用[ProdProduct]的[IsPublished]")]
		public bool IsSellable { get; set; }
		public int ShelfLifeDays { get; set; }

		[Display(Name = "有效日期")]
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

		[Display(Name = "製造日期")]
		[Required(ErrorMessage = "請選擇製造日期")]
		[DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}", ApplyFormatInEditMode = true)]
		public DateTime? ManufactureDate { get; set; }

		[Display(Name = "建檔人員ID")]
		public int? Creator { get; set; }

		[Display(Name = "建檔時間")]
		public DateTime CreatedDate { get; set; }

		[Display(Name = "最後異動人員ID")]
		public int? Reviser { get; set; }

		[Display(Name = "最後異動時間")]
		public DateTime? RevisedDate { get; set; }

		[Display(Name = "異動類型")]
		public string? MovementType { get; set; }

		[Display(Name = "手動調整數量是否為正數")]
		public bool IsAdd { get; set; }

		[Display(Name = "異動數量")]
		public int? ChangeQty { get; set; }  // 改成可空，由驗證控制

		[Display(Name = "異動備註")]
		public string? Remark { get; set; }

		public int? UserId { get; set; }
		public int SafetyStockQty { get; set; }
		public int ReorderPoint { get; set; }
		public int MaxStockQty { get; set; }
		public string? ProductName { get; set; }
		public string? BrandName { get; set; }
		public int? BrandId { get; set; }
		public int? ProductId { get; set; }
		public bool IsPublished { get; set; }   // [ProdProduct]的[IsPublished]

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
	}
}
