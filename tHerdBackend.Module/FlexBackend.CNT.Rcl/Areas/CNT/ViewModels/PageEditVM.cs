using FlexBackend.CNT.Rcl.Areas.CNT.Attributes;
using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels.Enums;
using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace FlexBackend.CNT.Rcl.Areas.CNT.ViewModels
{
	public static class CntConstants
	{
		public const int HomePageTypeId = 1000;
		// 允許的狀態代碼（對應 SystemCode：0=草稿,1=已發布,2=封存,9=刪除）
		public const string StatusCodePattern = "^(0|1|2|9)$";
	}
	public class PageEditVM
	{
		public int PageId { get; set; }           // 文章 ID

		[Required(ErrorMessage = "標題必填")]
		[StringLength(255, ErrorMessage = "標題長度不可超過 255 字")]
		public string Title { get; set; } = string.Empty;         // 標題
		[Range(1, int.MaxValue, ErrorMessage = "請選擇有效的頁面類型")]
		public int PageTypeId { get; set; }      // PageEditVM.cs
		[ValidateNever]
		public string PageTypeName { get; set; } = string.Empty;// 類型顯示用
		public DateTime CreatedDate { get; set; }  // 建立時間
		public DateTime? RevisedDate { get; set; } // 異動時間		

		//------------------------------------------------------------------------------------------
		// 由 enum 改為 代碼字串
		[Required(ErrorMessage = "狀態必填")]
		[RegularExpression(CntConstants.StatusCodePattern, ErrorMessage = "狀態代碼不合法")]
		public string StatusCode { get; set; } = "0";  // 狀態代碼（字串型態）預設Draft = "0"
		//------------------------------------------------------------------------------------------
		// 下拉清單（不驗證）
		[ValidateNever]
		public IEnumerable<SelectListItem> StatusList { get; set; } = Enumerable.Empty<SelectListItem>();
		// ⭐ 可供選擇的標籤清單（只做 UI 選項，不驗證）
		[ValidateNever]
		public IEnumerable<SelectListItem> TagOptions { get; set; } = Enumerable.Empty<SelectListItem>();
		//------------------------------------------------------------------------------------------
		// ⭐ 使用者選取的標籤 Id（多選）
		[Required(ErrorMessage = "標籤必填")]
		[MinLength(1, ErrorMessage = "至少選擇一個標籤")]
		public List<int> SelectedTagIds { get; set; } = new();
		//------------------------------------------------------------------------------------------
		// ⭐ 區塊列表 ⭐ 預設給一個空集合
		public List<PageBlockEditVM> Blocks { get; set; } = new();
		//------------------------------------------------------------------------------------------
		// 新增：是否設定排程
		public bool HasSchedule { get; set; } = false;
		[Display(Name = "排程動作")]
		public ActionType? ActionType { get; set; }
		// 排程欄位
		[Display(Name = "排程時間")]
		[DataType(DataType.DateTime)]
		[RequiredIf("HasSchedule", "排程時間必填")]
		public DateTime? ScheduledDate { get; set; }
		[ValidateNever]
		public IEnumerable<SelectListItem> ActionTypeList { get; set; } = new List<SelectListItem>();
		//------------------------------------------------------------------------------------------
		// 🔑 用來保留回列表的查詢條件，UI回填(查詢條件)
		public int? Page { get; set; }
		public int PageSize { get; set; } = 10;
		public string? Keyword { get; set; }
		public string? StatusFilter { get; set; }
		[ValidateNever]
		public bool IsHomePage => PageTypeId == CntConstants.HomePageTypeId;
	}
}
