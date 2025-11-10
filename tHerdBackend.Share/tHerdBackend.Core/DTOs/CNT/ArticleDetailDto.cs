using tHerdBackend.Core.DTOs.CNT;

namespace tHerdBackend.Share.DTOs.CNT
{
	/// <summary>
	/// 文章詳細資料 DTO（前台用）
	/// </summary>
	public class ArticleDetailDto
	{
		/// <summary>文章唯一識別碼（PageId）</summary>
		public int PageId { get; set; }

		/// <summary>文章標題</summary>
		public string Title { get; set; } = string.Empty;

		/// <summary>SEO 友善網址（Slug）；若無則以 PageId 代替</summary>
		public string Slug { get; set; } = string.Empty;

		/// <summary>SEO 標題（可為空）</summary>
		public string? SeoTitle { get; set; }

		/// <summary>SEO 描述（可為空）</summary>
		public string? SeoDesc { get; set; }

		/// <summary>發佈時間（未設定則為 DateTime.MinValue）</summary>
		public DateTime PublishedDate { get; set; }

		/// <summary>是否為付費內容（0=免費、1=付費）</summary>
		public bool IsPaidContent { get; set; }

		/// <summary>當前使用者是否已購買（僅在 IsPaidContent=true 時有意義）</summary>
		public bool HasPurchased { get; set; }

		/// <summary>免費預覽長度（字數）</summary>
		public int PreviewLength { get; set; }

		/// <summary>🔸 新增：單篇文章價格（對應 CNT_Page.Price）/// </summary>
		public decimal? Price { get; set; }

		/// <summary>標籤清單（顯示、導購用）</summary>
		public List<ArticleTagDto> Tags { get; set; } = new();

		/// <summary>
		/// 文章內容區塊（若為未購買付費內容，則 Blocks 為 null）
		/// 本 DTO 不再定義 PageBlockDto，改為使用專案內既有的 PageBlockDto 類別
		/// </summary>
		public List<PageBlockDto>? Blocks { get; set; }

		/// <summary>
		/// 文章分類代碼（PageTypeId）  
		/// 用於 Service 判斷同分類推薦文章
		/// </summary>
		public int PageTypeId { get; set; }
	}
}
