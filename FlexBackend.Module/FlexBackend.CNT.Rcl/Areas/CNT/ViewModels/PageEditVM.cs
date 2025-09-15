using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels.Enums;
using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace FlexBackend.CNT.Rcl.Areas.CNT.ViewModels
{
	public class PageEditVM
	{
		public int PageId { get; set; }           // 文章 ID

		[Required(ErrorMessage = "標題必填")]
		public string Title { get; set; }         // 標題

		[Required(ErrorMessage = "狀態必填")]
		[EnumDataType(typeof(PageStatus))]
		public PageStatus Status { get; set; }    // 改成 enum，而不是 string

		public DateTime? RevisedDate { get; set; } // 異動時間

		// 下拉選單使用（不驗證）
		[ValidateNever]
		public IEnumerable<SelectListItem> StatusList { get; set; }

		// ⭐ 使用者選取的標籤 Id（多選）
		public List<int> SelectedTagIds { get; set; } = new();

		// ⭐ 可供選擇的標籤清單（只做 UI 選項，不驗證）
		[ValidateNever]
		public IEnumerable<SelectListItem> TagOptions { get; set; }


		// ⭐ 區塊列表 ⭐ 預設給一個空集合
		public List<CntPageBlock> Blocks { get; set; } = new();

		// PageEditVM.cs
		public int PageTypeId { get; set; }


		// 🔑 用來保留回列表的查詢條件
		public int? Page { get; set; }
		public int PageSize { get; set; } = 8;
		public string? Keyword { get; set; }
		public string? StatusFilter { get; set; }

		// 狀態中文顯示
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
