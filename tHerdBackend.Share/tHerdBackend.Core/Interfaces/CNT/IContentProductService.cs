using tHerdBackend.Core.DTOs.CNT;

namespace tHerdBackend.Core.Interfaces.CNT
{
	public interface IContentProductService
	{
		Task<IReadOnlyList<ProductBriefDto>> GetRelatedProductsForPageAsync(
			int pageId,
			int take = 24);

		Task<IReadOnlyList<ProductBriefDto>> GetProductsByTagNameAsync(
			string tagName,
			int take = 24);
	}
}
