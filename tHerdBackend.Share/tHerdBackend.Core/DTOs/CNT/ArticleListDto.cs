using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Share.DTOs.CNT
{
	/// <summary>
	/// CNT 文章列表 DTO - 用於前台「文章列表 / 分類 / 搜尋結果」頁面
	/// </summary>
	public class ArticleListDto
	{
		/// <summary>
		/// 文章唯一 ID（用於取得詳細內容 / 路由）
		/// </summary>
		public int PageId { get; set; }

		/// <summary>
		/// 文章標題
		/// </summary>
		public required string Title { get; set; }

		/// <summary>
		/// SEO Slug（用於網址，例如：vitamin-c-benefits）
		/// 來源：SEO 表 (SeoSlug)
		/// </summary>
		public required string Slug { get; set; }

		/// <summary>
		/// 文章摘要（無摘要欄位時，自動擷取前段內文）
		/// </summary>
		public required string Excerpt { get; set; }

		/// <summary>
		/// 文章封面圖片（若無圖片，可給預設圖）
		/// </summary>
		public required string CoverImage { get; set; }

		/// <summary>
		/// 類別名稱（來源：CNT_PageType.TypeName）
		/// 用於顯示「健康 / 營養 / 運動」等分類
		/// </summary>
		public required string CategoryName { get; set; }

		/// <summary>
		/// 發佈日期（用於排序與顯示）
		/// </summary>
		public DateTime PublishedDate { get; set; }
	}
}

