using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;                // ← 這行一定要有，為了 ToListAsync
using tHerdBackend.Core.DTOs.CNT;
using tHerdBackend.Core.Interfaces.CNT;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Infra.Repository.CNT
{
	public class ProductTagRepository : IProductTagRepository
	{
		private readonly tHerdDBContext _db;

		public ProductTagRepository(tHerdDBContext db)
		{
			_db = db;
		}

		public async Task<IReadOnlyList<ProductBriefDto>> GetProductsByTagIdsAsync(
			IEnumerable<int> tagIds,
			int take = 24)
		{
			// 1. 整理傳進來的 tag 清單
			var tagIdList = tagIds
				.Distinct()
				.ToList();

			if (!tagIdList.Any())
				return new List<ProductBriefDto>();

			// 2. 從 CntProductTags 直接走導覽屬性 Product
			var query =
				from pt in _db.CntProductTags
				where tagIdList.Contains(pt.TagId)
					  && pt.IsVisible == true
					  && pt.IsDeleted == false
					  && pt.Product.IsPublished == true
				orderby
					pt.IsPrimary descending,                          // 主要標籤排前面
					pt.DisplayOrder ascending,                        // 後台手動排序
					(pt.Product.RevisedDate ?? pt.Product.CreatedDate) descending
				select new ProductBriefDto
				{
					ProductId = pt.Product.ProductId,
					ProductName = pt.Product.ProductName,
					ShortDesc = pt.Product.ShortDesc,
					Badge = pt.Product.Badge,
					MainSkuId = pt.Product.MainSkuId
				};

			return await query
				.Take(take)
				.ToListAsync();
		}
	}
}
