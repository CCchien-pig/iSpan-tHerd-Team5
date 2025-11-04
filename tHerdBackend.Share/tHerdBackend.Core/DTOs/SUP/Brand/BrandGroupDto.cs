namespace tHerdBackend.Core.DTOs.SUP.Brand
{
	public sealed class BrandGroupDto
	{
		public string Letter { get; set; } = "";
		public List<BrandGroupItemDto> Brands { get; set; } = new();
	}
}
