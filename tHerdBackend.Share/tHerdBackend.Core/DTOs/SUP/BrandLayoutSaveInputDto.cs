using System.ComponentModel.DataAnnotations;

namespace tHerdBackend.Core.DTOs.SUP
{
	/// <summary>
	/// 專門用於接收表單數據
	/// </summary>
	public class BrandLayoutSaveInputDto
	{
		public int BrandId { get; set; } // 從隱藏欄位取得
		public int? ActiveLayoutId { get; set; } // 如果是更新

		[Required(ErrorMessage = "版面配置內容不能為空。")]
		public string LayoutJson { get; set; } = string.Empty;
		public string? LayoutVersion { get; set; }
	}
}
