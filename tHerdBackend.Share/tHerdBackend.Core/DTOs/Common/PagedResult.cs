namespace tHerdBackend.Core.DTOs.Common
{
	/// <summary>
	/// 共用分頁結果
	/// </summary>
	public class PagedResult<T>
	{
		public int TotalCount { get; set; }     // 全部筆數
		public int PageIndex { get; set; }      // 第幾頁
		public int PageSize { get; set; }       // 每頁筆數
		public List<T> Items { get; set; } = new();
	}
}
