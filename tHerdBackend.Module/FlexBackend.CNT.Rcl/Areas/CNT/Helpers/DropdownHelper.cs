using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels.Enums;
using FlexBackend.Infra.Models;

namespace FlexBackend.CNT.Rcl.Helpers
{
	public static class DropdownHelper
	{
		// ================================
		// 狀態下拉選單
		// ================================
		public static IEnumerable<SelectListItem> GetStatusSelectList(
			PageStatus? selected = null,
			bool includeAll = false,
			bool includeDeleted = false)
		{
			var items = System.Enum.GetValues(typeof(PageStatus))
				.Cast<PageStatus>()
				.Where(s => includeDeleted || s != PageStatus.Deleted)
				.Select(s => new SelectListItem
				{
					Text = s switch
					{
						PageStatus.Draft => "草稿",
						PageStatus.Published => "已發佈",
						PageStatus.Archived => "封存",
						PageStatus.Deleted => "刪除",
						_ => "未知"
					},
					Value = ((int)s).ToString(),
					Selected = selected.HasValue && s == selected.Value
				}).ToList();

			if (includeAll)
			{
				items.Insert(0, new SelectListItem("全部狀態", "", !selected.HasValue));
			}

			return items;
		}

		// ================================
		// 分類下拉選單
		// ================================
		public static IEnumerable<SelectListItem> GetPageTypeSelectList(
			tHerdDBContext db,
			int? selected = null,
			bool includeAll = false)
		{
			var items = db.CntPageTypes
				.OrderBy(pt => pt.TypeName)
				.Select(pt => new SelectListItem
				{
					Text = pt.TypeName,
					Value = pt.PageTypeId.ToString(),
					Selected = selected.HasValue && pt.PageTypeId == selected.Value
				})
				.ToList();

			if (includeAll)
			{
				items.Insert(0, new SelectListItem("全部分類", "", !selected.HasValue));
			}

			return items;
		}
	}
}
