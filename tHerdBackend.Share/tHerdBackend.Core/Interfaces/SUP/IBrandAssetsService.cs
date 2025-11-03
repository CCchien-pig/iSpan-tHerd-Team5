using tHerdBackend.Core.DTOs.SUP.Brand;

namespace tHerdBackend.Core.Interfaces.SUP
{
	public interface IBrandAssetsService
	{
		Task<BrandContentImagesDto> GetRightImagesAsync(int brandId, int folderId, string? altText, CancellationToken ct);
	}
}
