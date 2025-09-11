using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexBackend.CNT.Rcl.Areas.CNT.ViewModels
{
	internal class PageListVM
	{
		//給 Index 頁面用的
		public int PageId { get; set; }       // 對應資料庫的 PageId
		public string Title { get; set; }     // 對應 Title
		public string Status { get; set; }       // 對應 Status (通常 0=草稿,1=已發佈...)
		public DateTime CreatedDate { get; set; } // 對應 CreatedDate

		// ★ 額外方便輸出的屬性 (避免 View 看到數字)
		public string StatusText
		{
			get
			{
				return Status switch
				{
					"0" => "草稿",
					"1" => "已發佈",
					"2" => "下架/封存",
					"9" => "已刪除",
					_ => "未知狀態"
				};
			}
		}

	}
}
