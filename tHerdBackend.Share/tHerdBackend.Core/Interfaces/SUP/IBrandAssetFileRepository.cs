using tHerdBackend.Core.DTOs.SUP.Brand;

namespace tHerdBackend.Core.Interfaces.SUP
{
	public interface IBrandAssetFileRepository
	{
		// Logo FolderId=56
		Task<IReadOnlyList<BrandLogoAssetDto>> GetActiveBrandLogosAsync(int folderId = 56);

	}
}
