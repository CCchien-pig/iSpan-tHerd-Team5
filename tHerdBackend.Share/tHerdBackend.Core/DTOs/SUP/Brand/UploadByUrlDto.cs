using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace tHerdBackend.Core.DTOs.SUP.Brand
{
	public class UploadByUrlDto
	{
		[Required]
		public string ImageUrl { get; set; }
		public string BlockType { get; set; }
		public string AltText { get; set; }
		public string Caption { get; set; }


		// ✅ 原本這行若是 bool 或 int 改成 string
		public string? IsActive { get; set; }

		// ✅ 新增這個轉換屬性
		[JsonIgnore]
		public bool IsActiveBool =>
			IsActive?.ToLower() == "true" || IsActive == "1";
	}
}
