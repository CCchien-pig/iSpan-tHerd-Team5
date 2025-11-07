using System;

namespace tHerdBackend.ORD.Rcl.Areas.ORD.ViewModels
{
	/// <summary>
	/// 退換貨列表 ViewModel
	/// </summary>
	public class ReturnListVM
	{
		/// <summary>
		/// 申請單編號
		/// </summary>
		public int ReturnRequestId { get; set; }

		/// <summary>
		/// 訂單ID（用於前端跳轉）
		/// </summary>
		public int OrderId { get; set; }

		/// <summary>
		/// RMA 單號
		/// </summary>
		public string RmaNo { get; set; } = "";

		/// <summary>
		/// 訂單號碼
		/// </summary>
		public string OrderNo { get; set; } = "";

		/// <summary>
		/// 申請類型（refund/reship）
		/// </summary>
		public string RequestType { get; set; } = "";

		/// <summary>
		/// 申請類型顯示名稱
		/// </summary>
		public string TypeName { get; set; } = "";

		/// <summary>
		/// 退換範圍（full/partial）
		/// </summary>
		public string Scope { get; set; } = "";

		/// <summary>
		/// 退換範圍顯示名稱
		/// </summary>
		public string ScopeName { get; set; } = "";

		/// <summary>
		/// 狀態（pending/review/refunding/done/rejected）
		/// </summary>
		public string Status { get; set; } = "";

		/// <summary>
		/// 狀態顯示名稱
		/// </summary>
		public string StatusName { get; set; } = "";

		/// <summary>
		/// 申請原因
		/// </summary>
		public string ReasonText { get; set; } = "";

		/// <summary>
		/// 審核意見
		/// </summary>
		public string? ReviewComment { get; set; }

		/// <summary>
		/// 建立時間
		/// </summary>
		public DateTime CreatedDate { get; set; }

		// 以下為列表顯示用（後台可能需要）
		/// <summary>
		/// 第一筆商品名稱
		/// </summary>
		public string ProductName { get; set; } = "";

		/// <summary>
		/// 第一筆商品規格
		/// </summary>
		public string Spec { get; set; } = "";

		/// <summary>
		/// 第一筆商品數量
		/// </summary>
		public int Qty { get; set; }
	}
}