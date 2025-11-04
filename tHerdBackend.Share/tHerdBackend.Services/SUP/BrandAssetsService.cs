using tHerdBackend.Core.DTOs.SUP.Brand;
using tHerdBackend.Core.Interfaces.SUP;

namespace tHerdBackend.Services.SUP
{
	public class BrandAssetsService : IBrandAssetsService
	{
		private readonly IBrandAssetsRepository _repo;
		private readonly IBrandRepository _brandsRepo; // 若需要取品牌名/驗證品牌存在

		public BrandAssetsService(IBrandAssetsRepository repo, IBrandRepository brandsRepo)
		{
			_repo = repo; _brandsRepo = brandsRepo;
		}

		public async Task<BrandContentImagesDto> GetRightImagesAsync(int brandId, int folderId, string? altText, CancellationToken ct)
		{
			// 若 altText 未給，可用品牌名稱當預設（可選）
			if (string.IsNullOrWhiteSpace(altText))
			{
				var (id, name, _) = await _brandsRepo.GetBrandAsync(brandId, ct);
				if (id != 0) altText = name;
			}

			var urls = await _repo.GetContentImagesAsync(brandId, folderId, altText, ct);
			return new BrandContentImagesDto { Urls = urls };
		}
	}
}
