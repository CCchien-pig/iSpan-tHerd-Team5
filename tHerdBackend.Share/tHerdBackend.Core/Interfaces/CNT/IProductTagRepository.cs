using tHerdBackend.Core.DTOs.CNT;
using tHerdBackend.Core.DTOs.PROD;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace tHerdBackend.Core.Interfaces.CNT
{
	// 舊功能（簡版商品卡片用）
	public interface IProductTagRepository
	{
		Task<IReadOnlyList<ProductBriefDto>> GetProductsByTagIdsAsync(
			IEnumerable<int> tagIds,
			int take = 24);
	}

	// 統一的分頁回傳格式，Controller 最後會回 { total, items }
	public class PagedResult<T>
	{
		public int Total { get; set; }
		public List<T> Items { get; set; } = new();
	}

	

}
