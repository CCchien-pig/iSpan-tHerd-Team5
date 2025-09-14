using FlexBackend.Infra.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexBackend.CNT.Rcl.Areas.CNT.ViewModels
{
	public class PageBlockEditVM
	{
		public int PageBlockId { get; set; }   // 編輯時用
		public int PageId { get; set; }        // 所屬文章 ID

		[Required]
		[Display(Name = "區塊類型")]
		public string BlockType { get; set; }  // richtext / image / video / cta

		[Display(Name = "內容")]
		public string Content { get; set; }    // 存 HTML 內容 (或圖片路徑)

		[Display(Name = "新區塊內容")]
		public string NewBlockContent { get; set; } // UI 專用 (不存 DB)

		//public string NewBlockType { get; set; } = "richtext"; // 預設文字

		[Display(Name = "排序")]
		public int OrderSeq { get; set; }

		// 🆕 Video Block
		[Display(Name = "影片連結")]
		public string? VideoUrl { get; set; }

		// 🆕 CTA Block
		[Display(Name = "CTA 按鈕文字")]
		public string? CtaText { get; set; }

		[Display(Name = "CTA 連結")]
		public string? CtaUrl { get; set; }
	}
}
