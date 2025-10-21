using System.ComponentModel.DataAnnotations;

namespace tHerdBackend.Core.DTOs.SUP
{
	public class BrandDiscountDto
	{
		/// <summary>
		/// 主鍵，自動編號
		/// </summary>
		[Display(Name = "品牌ID")]
		public int BrandId { get; set; }

		/// <summary>
		/// 品牌名稱
		/// </summary>
		[Display(Name = "品牌名稱")]
		public string BrandName { get; set; } = "";

		[Display(Name = "品牌折扣率")]
		[Range(0.01, 0.99, ErrorMessage = "品牌折扣率必須在0.01~0.99之間")]
		public decimal? DiscountRate { get; set; }

		[Display(Name = "折扣開始日期")]
		[Required(ErrorMessage = "請輸入折扣開始日期")]
		[DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}", ApplyFormatInEditMode = true)]

		public DateOnly? StartDate { get; set; }

		[Display(Name = "折扣結束日期")]
		[Required(ErrorMessage = "請輸入折扣結束日期")]
		[DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}", ApplyFormatInEditMode = true)]
		public DateOnly? EndDate { get; set; }

		/// <summary>
		/// 品牌折扣狀態，1=有效、0=結束
		/// </summary>
		[Display(Name = "折扣狀態")]
		public bool? IsDiscountActive { get; set; }
	}

}
