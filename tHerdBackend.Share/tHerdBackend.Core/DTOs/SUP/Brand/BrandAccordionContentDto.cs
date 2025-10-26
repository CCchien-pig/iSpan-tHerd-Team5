using tHerdBackend.Core.Interfaces.SUP;

namespace tHerdBackend.Core.DTOs.SUP.Brand
{
	public class BrandAccordionContentDto : IContentDto
	{
		/// <summary>
		/// 主鍵 Content Id
		/// </summary>
		public int ContentId { get; set; }

		/// <summary>
		/// 關聯品牌
		/// </summary>
		public int? BrandId { get; set; }

		/// <summary>
		/// 摺疊標題
		/// </summary>
		public string ContentTitle { get; set; }

		/// <summary>
		/// 摺疊內文
		/// </summary>
		public string Content { get; set; }

		/// <summary>
		/// 顯示順序
		/// </summary>
		public int OrderSeq { get; set; }

		/// <summary>
		/// 品牌摺疊內容圖片
		/// </summary>
		public int? ImgId { get; set; }

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
