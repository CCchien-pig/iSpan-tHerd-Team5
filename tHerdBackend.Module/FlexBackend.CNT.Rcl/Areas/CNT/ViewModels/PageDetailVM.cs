using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels.Enums;
using FlexBackend.Infra.Models;
using System;
using System.Collections.Generic;

namespace FlexBackend.CNT.Rcl.Areas.CNT.ViewModels
{
	public class PageDetailVM
	{
		public int PageId { get; set; }
		public string Title { get; set; }
		public PageStatus Status { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime? RevisedDate { get; set; }

		//頁面類型
		public int PageTypeId { get; set; }
		public string PageTypeName { get; set; } = "";  // ⭐ 類別顯示用
		public bool IsHomePage => PageTypeId == 1000;

		// ⭐ 顯示標籤名稱（不是 Id）
		public List<string> TagNames { get; set; } = new();

		// ⭐ 加上 Blocks
		public List<CntPageBlock> Blocks { get; set; } = new();

		// ⭐ 新增：排程清單
		public List<ScheduleVM> Schedules { get; set; } = new();

		// 🔑 新增：用來保留分頁與篩選狀態
		public int? Page { get; set; }
		public int PageSize { get; set; } = 8;
		public string? Keyword { get; set; }
		public string? StatusFilter { get; set; }

		// ⭐ 額外：讓 Razor 直接顯示中文狀態，不用再寫判斷
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
