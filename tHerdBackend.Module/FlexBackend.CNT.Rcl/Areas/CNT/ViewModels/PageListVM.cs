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

		public PageStatus Status { get; set; }   // 用 enum 取代 string

		public DateTime CreatedDate { get; set; }
		public DateTime? RevisedDate { get; set; }

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

