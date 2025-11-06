namespace tHerdBackend.Core.DTOs.CNT
{
	/// <summary>購買紀錄摘要 – 建立/查詢用</summary>
	public class PurchaseSummaryDto
	{
		public int PurchaseId { get; set; }
		public int PageId { get; set; }
		public decimal Amount { get; set; }

		public bool IsPaid { get; set; }
		public string PaymentMethod { get; set; } = "";
		public string PaymentStatus { get; set; } = "";

		/// <summary>
		/// 之後串金流時用來回傳跳轉網址（LinePay paymentUrl 或綠界的表單資訊）
		/// 目前可以先留 null
		/// </summary>
		public string? PaymentUrl { get; set; }
	}
}
