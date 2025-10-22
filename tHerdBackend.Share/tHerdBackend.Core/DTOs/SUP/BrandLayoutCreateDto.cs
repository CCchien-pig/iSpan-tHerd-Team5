namespace tHerdBackend.Core.DTOs.SUP
{
	/// <summary>
	/// 品牌版面 - 新增用資料傳輸物件
	/// </summary>
	public class BrandLayoutCreateDto
	{
		/// <summary>
		/// 版面內容（JSON Array 格式）
		/// </summary>
		public string LayoutJson { get; set; } = string.Empty;

		/// <summary>
		/// 版型版本（可空）
		/// </summary>
		public string? LayoutVersion { get; set; }

		/// <summary>
		/// 建檔人員（後台登入使用者 ID）
		/// </summary>
		public int Creator { get; set; }
	}
}

