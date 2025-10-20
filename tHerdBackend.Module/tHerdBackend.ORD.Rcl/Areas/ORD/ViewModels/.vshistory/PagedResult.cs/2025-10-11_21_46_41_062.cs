using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexBackend.ORD.Rcl.Areas.ORD.ViewModels
{
	public class PagedResult<T>
	{
		public List<T> Items { get; set; } = new List<T>();
		public int TotalItems { get; set; }
		public int Page { get; set; }
		public int PageSize { get; set; }

		// 排序相關
		public string Sort { get; set; }
		public string Dir { get; set; }

		// 查詢相關
		public string Query { get; set; }
		public bool ShowHidden { get; set; }

		// 頁數計算方便用
		public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
	}
}
