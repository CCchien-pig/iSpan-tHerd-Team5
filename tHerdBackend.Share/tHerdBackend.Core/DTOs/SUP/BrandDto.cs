using System;
using System.ComponentModel.DataAnnotations;

namespace tHerdBackend.Core.DTOs.SUP
{
	public class BrandDto
	{
		/// <summary>
		/// 主鍵，自動編號
		/// </summary>
		[Display(Name = "品牌ID")]
		public int BrandId { get; set; }

		/// <summary>
		/// 品牌名稱
		/// </summary>
		[Display(Name = "品牌名稱")]
		public string BrandName { get; set; }

		/// <summary>
		/// 品牌簡碼，唯一，非空，英文
		/// </summary>
		[Display(Name = "品牌簡碼")]
		public string BrandCode { get; set; }

		/// <summary>
		/// 品牌關聯供應商Id
		/// </summary>
		[Display(Name = "供應商ID")]
		public int? SupplierId { get; set; }

		[Display(Name = "供應商名稱")]
		public string? SupplierName { get; set; } // 顯示用途，查詢JOIN SUP_Supplier時填入用

		/// <summary>
		/// Seo 設定
		/// </summary>
		[Display(Name = "設定SeoId")]
		public int? SeoId { get; set; }
		//public string? SeoId { get; set; }

		/// <summary>
		/// 折扣率 (0 ~ 100%)
		/// </summary>
		[Display(Name = "品牌折扣率")]
		public decimal? DiscountRate { get; set; }

		/// <summary>
		/// 品牌折扣狀態，1=有效、0=結束
		/// </summary>
		[Display(Name = "折扣狀態")]
		public bool? IsDiscountActive { get; set; }

		public string? DiscountStatus { get; set; } // 新增給前端用

		/// <summary>
		/// 折扣開始日期
		/// </summary>
		[Display(Name = "折扣開始日期")]
		public DateOnly? StartDate { get; set; }

		/// <summary>
		/// 折扣結束日期
		/// </summary>
		[Display(Name = "折扣結束日期")]
		public DateOnly? EndDate { get; set; }

		/// <summary>
		/// 是否為重點展示品牌(精選品牌狀態)
		/// </summary>
		[Display(Name = "精選品牌")]
		public bool IsFeatured { get; set; }

		/// <summary>
		/// 品牌按讚數（快取）
		/// </summary>
		[Display(Name = "按讚數")]
		public int LikeCount { get; set; }

		/// <summary>
		/// 品牌是否啟用
		/// </summary>
		[Display(Name = "品牌啟用狀態")]
		public bool IsActive { get; set; }

		/// <summary>
		/// 建檔人員
		/// </summary>
		[Display(Name = "建檔人員ID")]
		public int? Creator { get; set; }

		/// <summary>
		/// 建檔時間
		/// </summary>
		[Display(Name = "建檔時間")]
		[DisplayFormat(DataFormatString = "{0:yyyy/MM/dd tt hh:mm:ss}", ApplyFormatInEditMode = true)]
		public DateTime CreatedDate { get; set; }

		/// <summary>
		/// 異動人員
		/// </summary>
		[Display(Name = "最後異動人員ID")]
		public int? Reviser { get; set; }

		/// <summary>
		/// 異動時間
		/// </summary>
		[Display(Name = "最後異動時間")]
		[DisplayFormat(DataFormatString = "{0:yyyy/MM/dd tt hh:mm:ss}", ApplyFormatInEditMode = true)]
		public DateTime? RevisedDate { get; set; }
	}
}
