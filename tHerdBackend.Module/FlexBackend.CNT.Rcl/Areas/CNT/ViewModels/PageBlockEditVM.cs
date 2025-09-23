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
		public string? BlockType { get; set; }  // richtext / image / video / cta

		[Display(Name = "內容 (DB 儲存用)")]
		public string? Content { get; set; }   // 真正存進 DB 的值

		[Display(Name = "新區塊內容")]
		public string? NewBlockContent { get; set; } // RichText 用 (前端 <textarea> 綁定)

		[Display(Name = "排序")]
		[Range(1, int.MaxValue, ErrorMessage = "排序必須 ≥ 1")]
		public int OrderSeq { get; set; } = 1; // 排序

		// Video Block
		[Display(Name = "影片連結")]
		[StringLength(2048, ErrorMessage = "影片連結過長")]
		[Url(ErrorMessage = "請輸入正確的網址")]
		public string? VideoUrl { get; set; }

		// CTA Block
		[Display(Name = "CTA 按鈕文字")]
		[StringLength(100, ErrorMessage = "CTA 文字過長")]
		public string? CtaText { get; set; }

		[Display(Name = "CTA 連結")]
		[StringLength(2048, ErrorMessage = "CTA 連結過長")]
		[Url(ErrorMessage = "請輸入正確的網址")]
		public string? CtaUrl { get; set; }

		// === 中英對照表 ===
		public static class BlockTypeMap
		{
			public static readonly Dictionary<string, string> DisplayToDb = new()
		{
			{ "文字編輯器", "richtext" },
			{ "圖片", "image" },
			{ "影片", "video" },
			{ "行動按鈕", "cta" }
		};

			public static readonly Dictionary<string, string> DbToDisplay =
				DisplayToDb.ToDictionary(kv => kv.Value, kv => kv.Key);
		}
		// ===========================
		// 動態驗證：依 BlockType 判斷
		// ===========================
		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			switch (BlockType)
			{
				case "文字編輯器":
					if (string.IsNullOrWhiteSpace(NewBlockContent))
						yield return new ValidationResult("請輸入文字內容", new[] { nameof(NewBlockContent) });
					break;

				case "圖片":
					// IFormFile 驗證交給 Controller
					break;

				case "影片":
					if (string.IsNullOrWhiteSpace(VideoUrl))
						yield return new ValidationResult("影片連結必填", new[] { nameof(VideoUrl) });
					break;

				case "行動按鈕":
					if (string.IsNullOrWhiteSpace(CtaText))
						yield return new ValidationResult("CTA 按鈕文字必填", new[] { nameof(CtaText) });
					if (string.IsNullOrWhiteSpace(CtaUrl))
						yield return new ValidationResult("CTA 連結必填", new[] { nameof(CtaUrl) });
					break;

				default:
					yield return new ValidationResult("不支援的區塊類型", new[] { nameof(BlockType) });
					break;
			}
		}
	}
}
