namespace tHerdBackend.Core.DTOs.SUP.Brand
{
	public sealed class BrandLogoAssetDto
	{
		public int FileId { get; set; }
		public string AltText { get; set; } = "";
		public string? FileUrl { get; set; }
		public DateTime CreatedDate { get; set; }
	}
}
