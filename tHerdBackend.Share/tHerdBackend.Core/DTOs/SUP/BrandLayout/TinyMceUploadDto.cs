using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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


		public string? IsActive { get; set; }  // 因為前端傳的 IsActive 是字串，改為 string

		// ✅ 新增這個轉換屬性
		[JsonIgnore]
		public bool IsActiveBool =>
			IsActive?.ToLower() == "true" || IsActive == "1";
	}
}