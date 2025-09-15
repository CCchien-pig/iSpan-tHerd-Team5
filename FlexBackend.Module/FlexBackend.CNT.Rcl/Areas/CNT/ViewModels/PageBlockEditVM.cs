using FlexBackend.Infra.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexBackend.CNT.Rcl.Areas.CNT.ViewModels
{
	public class PageBlockEditVM : IValidatableObject
	{
		public int PageBlockId { get; set; }   // 編輯時用
		public int PageId { get; set; }        // 所屬文章 ID

		[Required(ErrorMessage = "區塊類型必填")]
		[Display(Name = "區塊類型")]
		public string BlockType { get; set; }  // richtext / image / video / cta

		[Display(Name = "內容 (DB 儲存用)")]
		public string? Content { get; set; }   // 真正存進 DB 的值

		[Display(Name = "新區塊內容")]
		public string? NewBlockContent { get; set; } // RichText 用 (前端 <textarea> 綁定)

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

		// ===========================
		// 動態驗證：依 BlockType 判斷
		// ===========================
		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			switch (BlockType?.ToLower())
			{
				case "richtext":
					if (string.IsNullOrWhiteSpace(NewBlockContent))
						yield return new ValidationResult("請輸入文字內容", new[] { nameof(NewBlockContent) });
					break;

				case "image":
					// Image 交給 Controller 的 IFormFile 驗證，這裡不做 Required
					break;

				case "video":
					if (string.IsNullOrWhiteSpace(VideoUrl))
						yield return new ValidationResult("影片連結必填", new[] { nameof(VideoUrl) });
					break;

				case "cta":
					if (string.IsNullOrWhiteSpace(CtaText))
						yield return new ValidationResult("CTA 按鈕文字必填", new[] { nameof(CtaText) });
					if (string.IsNullOrWhiteSpace(CtaUrl))
						yield return new ValidationResult("CTA 連結必填", new[] { nameof(CtaUrl) });
					break;
			}
		}
	}
}
