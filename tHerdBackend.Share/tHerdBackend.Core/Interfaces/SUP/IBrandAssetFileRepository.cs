using tHerdBackend.Core.DTOs.SUP.Brand;

namespace tHerdBackend.Core.Interfaces.SUP
{
	/// <summary>
	/// 專責「Logo 清單」等固定場景（FolderId=56），輸出 BrandLogoAssetDto。
	/// </summary>
	public interface IBrandAssetFileRepository
	{
		// Logo 專用，FolderId=56
		Task<IReadOnlyList<BrandLogoAssetDto>> GetActiveBrandLogosAsync(int folderId = 56);

	}
}
