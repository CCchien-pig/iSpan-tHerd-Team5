using tHerdBackend.Infra.Models.Sup;

namespace tHerdBackend.Core.Interfaces.SUP
{
	public interface ISupBrandsService
	{
		Task<BrandDetailDto?> GetBrandDetailAsync(int brandId, CancellationToken ct);
	}
}
