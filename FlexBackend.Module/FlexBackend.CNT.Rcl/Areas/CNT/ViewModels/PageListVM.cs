using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels.Enums;

namespace FlexBackend.CNT.Rcl.Areas.CNT.ViewModels
{
	public class PageListVM
	{
		public int PageId { get; set; }
		public string Title { get; set; }
		public string PageTypeName { get; set; } // ⭐ 新增文章類型名稱

		public PageStatus Status { get; set; }   // 用 enum 取代 string

		public DateTime CreatedDate { get; set; }
		public DateTime? RevisedDate { get; set; }

		// 額外：根據 PageTypeName 決定 CSS 類別與圖示
		public string PageTypeCssClass =>
			PageTypeName switch
			{
				"首頁" => "bg-primary",
				"文章" => "bg-info text-dark",
				"活動" => "bg-warning text-dark",
				_ => "bg-secondary"
			};

		public string PageTypeIcon =>
			PageTypeName switch
			{
				"首頁" => "bi-house-door-fill",
				"文章" => "bi-journal-text",
				"活動" => "bi-megaphone",
				_ => "bi-file-earmark-text"
			};


		// 額外輸出文字
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

