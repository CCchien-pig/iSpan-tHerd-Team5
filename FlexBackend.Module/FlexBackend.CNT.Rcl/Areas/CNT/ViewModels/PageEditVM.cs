using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexBackend.CNT.Rcl.Areas.CNT.ViewModels
{
	public class PageEditVM
	{
		public int PageId { get; set; }           // 文章 ID
		public string Title { get; set; }         // 標題
		public string Status { get; set; }        // 狀態 (資料庫是 varchar)

		public DateTime? RevisedDate { get; set; } // 異動時間 (系統自動更新)


		// 額外方便輸出的屬性
		public string StatusText =>
			Status switch
			{
				"0" => "草稿",
				"1" => "已發佈",
				"2" => "封存",
				"9" => "刪除",
				_ => "未知狀態"
			};
	}
}

