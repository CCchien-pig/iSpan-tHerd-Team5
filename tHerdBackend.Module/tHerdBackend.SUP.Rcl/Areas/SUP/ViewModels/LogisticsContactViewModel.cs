using System.ComponentModel.DataAnnotations;

namespace FlexBackend.SUP.Rcl.Areas.SUP.ViewModels
{
	public class LogisticsContactViewModel
	{
		/// <summary>
		/// 物流商主鍵
		/// </summary>
		[Display(Name = "物流商Id")]
		public int LogisticsId { get; set; }

		/// <summary>
		/// 物流商名稱
		/// </summary>
		[Display(Name = "物流商名稱")]
		[Required(ErrorMessage = "請輸入物流商名稱")]
		[StringLength(50, ErrorMessage = "物流商名稱不可超過50字")]
		public string? LogisticsName { get; set; }

		/// <summary>
		/// 配送方式
		/// </summary>
		[Display(Name = "配送方式")]
		[Required(ErrorMessage = "請輸入配送方式")]
		[StringLength(50, ErrorMessage = "配送方式不可超過50字")]
		public string? ShippingMethod { get; set; }

		/// <summary>
		/// 是否啟用
		/// </summary>
		[Display(Name = "啟用狀態")]
		public bool IsActive { get; set; } = true;

		// 唯讀欄位

		[Display(Name = "建檔人員ID")]
		public int? Creator { get; set; }

		[Display(Name = "建檔時間")]
		[DisplayFormat(DataFormatString = "{0:yyyy/MM/dd tt hh:mm:ss}", ApplyFormatInEditMode = true)]
		public DateTime CreatedDate { get; set; }

		[Display(Name = "最後異動人員ID")]
		public int? Reviser { get; set; }

		[Display(Name = "最後異動時間")]
		[DisplayFormat(DataFormatString = "{0:yyyy/MM/dd tt hh:mm:ss}", ApplyFormatInEditMode = true)]
		public DateTime? RevisedDate { get; set; }
	}
}
