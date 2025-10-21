using System.ComponentModel.DataAnnotations;

namespace tHerdBackend.Core.DTOs.SUP
{
	public class BrandDiscountDto
	{
		[Required]
		[Display(Name = "供應商ID")]
		public int BrandId { get; set; }

		[Display(Name = "品牌折扣率")]
		[Range(0.01, 0.99, ErrorMessage = "品牌折扣率必須在0.01~0.99之間")]
		public decimal? DiscountRate { get; set; }

		[Required]
		[Display(Name = "折扣開始日期")]
		[DisplayFormat(DataFormatString = "{0:yyyy/MM/dd tt hh:mm:ss}", ApplyFormatInEditMode = true)]
		public DateOnly? StartDate { get; set; }

		[Required]
		[Display(Name = "折扣結束日期")]
		[DisplayFormat(DataFormatString = "{0:yyyy/MM/dd tt hh:mm:ss}", ApplyFormatInEditMode = true)]
		public DateOnly? EndDate { get; set; }
	}

}
