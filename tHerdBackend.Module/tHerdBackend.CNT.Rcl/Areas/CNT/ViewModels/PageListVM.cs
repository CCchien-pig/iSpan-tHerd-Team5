using System;
using System.Collections.Generic;
using tHerdBackend.CNT.Rcl.Areas.CNT.ViewModels.Enums;

namespace tHerdBackend.CNT.Rcl.Areas.CNT.ViewModels
{
	public class PageListVM
	{
		public int PageId { get; set; }
		public string Title { get; set; }
		public string PageTypeName { get; set; } // ⭐ 文章類型名稱
		public PageStatus Status { get; set; }   // 用 enum 取代 string

		public DateTime CreatedDate { get; set; }
		public DateTime? RevisedDate { get; set; }

		// 文章分類：顏色
		public string PageTypeBadgeClass =>
		PageTypeName switch
		{
			"首頁" => "bg-primary text-white",   
			"極受歡迎" => "bg-danger text-white",   
			"健身" => "bg-info text-dark",   
			"營養" => "bg-warning text-dark",    
			"美容美妝" => "bg-pink text-white",      
			"文章" => "bg-success text-white",       
			"影片" => "bg-dark text-white",
			"健康專家" => "bg-purple text-white",    
			_ => "bg-secondary text-white"  
		};

		// ⭐ 狀態：顏色樣式
		public string StatusBadgeClass =>
			Status switch
			{
				PageStatus.Draft => "bg-secondary text-white",
				PageStatus.Published => "bg-success text-white",
				PageStatus.Archived => "bg-warning text-white",
				PageStatus.Deleted => "bg-danger text-white",
				_ => "bg-dark text-white"
			};


		// 文章分類：圖示
		public string PageTypeIcon =>
			PageTypeName switch
			{
				"首頁" => "bi-house-door-fill",
				"文章" => "bi-journal-text",
				"活動" => "bi-megaphone",
				_ => "bi-file-earmark-text"
			};

		// 狀態：文字
		public string StatusText =>
			Status switch
			{
				PageStatus.Draft => "草稿",
				PageStatus.Published => "已發佈",
				PageStatus.Archived => "封存",
				PageStatus.Deleted => "刪除",
				_ => "未知"
			};
	}
}
