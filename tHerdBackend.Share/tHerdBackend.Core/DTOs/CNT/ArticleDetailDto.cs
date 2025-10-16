using System;
using System.Collections.Generic;

namespace tHerdBackend.Share.DTOs.CNT
{
	/// <summary>
	/// CNT 單篇文章 DTO - 用於前台顯示文章詳細內容
	/// </summary>
	public class ArticleDetailDto
	{
		/// <summary>
		/// 文章唯一 ID
		/// </summary>
		public int PageId { get; set; }

		/// <summary>
		/// 文章標題
		/// </summary>
		public required string Title { get; set; }

		/// <summary>
		/// SEO 用 Slug（網址結尾）
		/// 來源：SEO 表 (SeoSlug)
		/// </summary>
		public required string Slug { get; set; }

		/// <summary>
		/// SEO 標題（若無可使用 Title）
		/// 用於 <title> 或社群分享
		/// </summary>
		public string? SeoTitle { get; set; }

		/// <summary>
		/// SEO 描述（Meta Description）
		/// </summary>
		public string? SeoDesc { get; set; }

		/// <summary>
		/// 發佈日期（顯示與排序）
		/// </summary>
		public DateTime PublishedDate { get; set; }

		/// <summary>
		/// 是否為付費文章（True = 需付費解鎖）
		/// </summary>
		public bool IsPaidContent { get; set; }

		/// <summary>
		/// 使用者是否已購買此文章
		/// 需登入後才能判斷
		/// </summary>
		public bool HasPurchased { get; set; }

		/// <summary>
		/// 可顯示的預覽字數（用於未購買時）
		/// 來源：CNT_Page.PreviewLength 或預設值 (150)
		/// </summary>
		public int PreviewLength { get; set; }

		/// <summary>
		/// 文章內容區塊（若未購買且為付費文章，Blocks 可能為空或僅前幾段）
		/// </summary>
		public List<PageBlockDto> Blocks { get; set; } = new();

		/// <summary>
		/// 標籤（Tags）
		/// 用於導購商品或延伸閱讀，例如：魚油、維生素C...
		/// </summary>
		public List<string> Tags { get; set; } = new();
	}
}
