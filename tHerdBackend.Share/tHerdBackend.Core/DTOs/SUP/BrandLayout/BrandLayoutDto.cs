namespace tHerdBackend.Core.DTOs.SUP.BrandLayout
{
	public class BrandLayoutDto
	{
		/// <summary>
		/// 版面設定 ID
		/// </summary>
		public int LayoutId { get; set; }

		/// <summary>
		/// 關聯品牌 ID
		/// </summary>
		public int BrandId { get; set; }

		/// <summary>
		/// 版面內容（JSON Array 格式，記錄 type/props/id）
		/// </summary>
		public string LayoutJson { get; set; }

		/// <summary>
		/// 版型版本
		/// </summary>
		public string LayoutVersion { get; set; }

		/// <summary>
		/// 是否啟用（0=否，1=是）
		/// </summary>
		public bool IsActive { get; set; }

		/// <summary>
		/// 建檔人員
		/// </summary>
		public int Creator { get; set; }

		/// <summary>
		/// 建立時間
		/// </summary>
		public DateTime CreatedDate { get; set; }

		/// <summary>
		/// 異動人員
		/// </summary>
		public int? Reviser { get; set; }

		/// <summary>
		/// 異動時間
		/// </summary>
		public DateTime? RevisedDate { get; set; }
	}
}
