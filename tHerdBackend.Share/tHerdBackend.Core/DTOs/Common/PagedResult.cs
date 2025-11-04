
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace tHerdBackend.Core.DTOs.Common
{
	/// <summary>
	/// 共用分頁結果
	/// </summary>
	public class PagedResult<T>
	{
		public PagedResult() { }

		public PagedResult(IEnumerable<T> items, int totalCount, int pageIndex, int pageSize)
		{
			Items = items?.ToList() ?? new List<T>();
			TotalCount = totalCount;
			PageIndex = pageIndex;
			PageSize = pageSize;
		}

		// —— 你原本的屬性（舊鍵名）——
		public int TotalCount { get; set; }     // 全部筆數
		public int PageIndex { get; set; }      // 第幾頁（從 1 起）
		public int PageSize { get; set; }       // 每頁筆數
		public List<T> Items { get; set; } = new();

		// —— 兼容新鍵名（只讀映射，不重複存值，避免不同步）——
		[JsonPropertyName("total")]
		public int Total => TotalCount;

		[JsonPropertyName("page")]
		public int Page => PageIndex;

		// 若前端希望鍵名是小寫 items，就不需要處理；已是 Items → items（預設 camelCase）
		// 若你有啟用 System.Text.Json 的 camelCase（預設會把 Items 變成 items）
		// 就不用再加別名。這裡保留 Items 即可。
	}
}
