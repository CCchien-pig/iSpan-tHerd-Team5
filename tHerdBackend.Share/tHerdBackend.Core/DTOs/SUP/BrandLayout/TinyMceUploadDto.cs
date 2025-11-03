using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace tHerdBackend.Core.DTOs
{
	/// <summary>
	/// 用於接收 TinyMCE 透過 Form-Data 上傳檔案及相關資訊的 DTO
	/// </summary>
	public class TinyMceUploadDto
	{
		[Required(ErrorMessage = "必須上傳檔案。")]
		public IFormFile File { get; set; }

		[Required(ErrorMessage = "必須提供區塊類型。")]
		public string BlockType { get; set; }

		public string AltText { get; set; }

		public string Caption { get; set; }

		public bool IsActive { get; set; } = true;
	}
}