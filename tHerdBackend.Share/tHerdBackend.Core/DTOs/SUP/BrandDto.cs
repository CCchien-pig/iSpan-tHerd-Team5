namespace tHerdBackend.Core.DTOs.SUP
{
	public class BrandDto
	{/// <summary>
	 /// 主鍵，自動編號
	 /// </summary>
		public int BrandId { get; set; }

		/// <summary>
		/// 品牌名稱
		/// </summary>
		public string BrandName { get; set; }

		/// <summary>
		/// 品牌簡碼，唯一，非空，英文
		/// </summary>
		public string BrandCode { get; set; }

		/// <summary>
		/// 關聯供應商
		/// </summary>
		public int? SupplierId { get; set; }

		/// <summary>
		/// Seo 設定
		/// </summary>
		public int? SeoId { get; set; }

		/// <summary>
		/// 折扣率 (0 ~ 100%)
		/// </summary>
		public decimal? DiscountRate { get; set; }

		/// <summary>
		/// 折扣狀態，1=有效、0=結束（排程更新）
		/// </summary>
		public bool IsDiscountActive { get; set; }

		/// <summary>
		/// 折扣開始日期
		/// </summary>
		public DateOnly? StartDate { get; set; }

		/// <summary>
		/// 折扣結束日期
		/// </summary>
		public DateOnly? EndDate { get; set; }

		/// <summary>
		/// 是否為重點展示品牌
		/// </summary>
		public bool IsFeatured { get; set; }

		/// <summary>
		/// 按讚數（快取）
		/// </summary>
		public int LikeCount { get; set; }

		/// <summary>
		/// 是否啟用
		/// </summary>
		public bool IsActive { get; set; }

		/// <summary>
		/// 建檔人員
		/// </summary>
		public int? Creator { get; set; }

		/// <summary>
		/// 建檔時間
		/// </summary>
		public DateTime CreatedDate { get; set; }

		/// <summary>
		/// 異動人員
		/// </summary>
		public int? Reviser { get; set; }

		/// <summary>
		/// 異動時間
		/// </summary>
		public DateTime? RevisedDate { get; set; }
	}
}
