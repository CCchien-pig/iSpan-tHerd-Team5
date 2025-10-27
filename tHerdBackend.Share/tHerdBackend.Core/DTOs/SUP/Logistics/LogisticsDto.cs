namespace tHerdBackend.Core.DTOs.SUP.Logistics
{
	public class LogisticsDto
	{
		/// <summary>
		/// 主鍵
		/// </summary>
		public int LogisticsId { get; set; }

		public string ShippingMethod { get; set; }

		/// <summary>
		/// 物流商名稱
		/// </summary>
		public string LogisticsName { get; set; }

		/// <summary>
		/// 是否啟用
		/// </summary>
		public bool IsActive { get; set; }

		/// <summary>
		/// 建檔人員
		/// </summary>
		public int? Creator { get; set; }

		/// <summary>
		/// 建檔時間
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