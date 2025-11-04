namespace tHerdBackend.Core.DTOs.SUP.Brand
{
	public sealed class BrandGroupItemDto
	{
		public int BrandId { get; set; }
		public string BrandName { get; set; } = "";
		public string BrandCode { get; set; } = "";
		public bool IsActive { get; set; }
		public bool IsFeatured { get; set; }
		public decimal? DiscountRate { get; set; }
		public bool? IsDiscountActive { get; set; }
		public string? LogoUrl { get; set; }
	}
}
