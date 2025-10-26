namespace tHerdBackend.Core.DTOs.SUP.BrandLayout
{
	/// <summary>
	/// 品牌版面 - 更新用資料傳輸物件 (PUT/PATCH)
	/// </summary>
	public class BrandLayoutUpdateDto
	{
		/// <summary>
		/// 版面內容（JSON Array 格式）
		/// </summary>
		public string LayoutJson { get; set; } = string.Empty;

		/// <summary>
		/// 版型版本（可選）
		/// </summary>
		public string? LayoutVersion { get; set; }

		/// <summary>
		/// 異動人員（後台登入使用者 ID）
		/// </summary>
		public int Reviser { get; set; }
	}
}
