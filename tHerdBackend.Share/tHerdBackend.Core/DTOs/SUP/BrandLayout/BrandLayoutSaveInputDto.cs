using System.ComponentModel.DataAnnotations;

namespace tHerdBackend.Core.DTOs.SUP.BrandLayout
{
	/// <summary>
	/// 專門用於接收表單數據
	/// </summary>
	public class BrandLayoutSaveInputDto
	{
		public int BrandId { get; set; } // 從隱藏欄位取得
		public int? ActiveLayoutId { get; set; } // 如果是更新
		public string? LayoutVersion { get; set; }

		// 將屬性名稱從 LayoutJson 改為 FullLayoutJson，以接收前端傳來的完整 JSON
		// 將類型從 string 改為 List<BaseLayoutBlockDto>
		[Required(ErrorMessage = "版面配置內容不能為空。")]
		public List<BaseLayoutBlockDto> FullLayoutJson { get; set; } = new List<BaseLayoutBlockDto>();
	}
}
