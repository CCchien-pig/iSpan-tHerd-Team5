using tHerdBackend.Core.DTOs.CNT;

namespace tHerdBackend.Core.Interfaces.CNT
{
	public interface IProductTagRepository
	{
		Task<IReadOnlyList<ProductBriefDto>> GetProductsByTagIdsAsync(
			IEnumerable<int> tagIds,
			int take = 24);
	}
}
