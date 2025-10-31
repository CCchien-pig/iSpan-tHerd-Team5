using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.CNT;
using tHerdBackend.Core.Interfaces.CNT;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Services.CNT
{
	public class ContentProductService : IContentProductService
	{
		private readonly tHerdDBContext _db;
		private readonly IProductTagRepository _productTagRepo;

		public ContentProductService(
			tHerdDBContext db,
			IProductTagRepository productTagRepo)
		{
			_db = db;
			_productTagRepo = productTagRepo;
		}

		public async Task<IReadOnlyList<ProductBriefDto>> GetRelatedProductsForPageAsync(
			int pageId,
			int take = 24)
		{
			// 文章 → 找文章的 TagId 列表
			var tagIds = await _db.CntPageTags
								  .Where(x => x.PageId == pageId)
								  .Select(x => x.TagId)
								  .Distinct()
								  .ToListAsync();

			return await _productTagRepo.GetProductsByTagIdsAsync(tagIds, take);
		}

		public async Task<IReadOnlyList<ProductBriefDto>> GetProductsByTagNameAsync(
			string tagName,
			int take = 24)
		{
			if (string.IsNullOrWhiteSpace(tagName))
				return Array.Empty<ProductBriefDto>();

			// 標籤名稱 -> 對應的 TagId 集合
			var tagIds = await _db.CntTags
								  .Where(t => t.TagName == tagName && t.IsActive == true)
								  .Select(t => t.TagId)
								  .Distinct()
								  .ToListAsync();

			if (!tagIds.Any())
				return Array.Empty<ProductBriefDto>();

			// 再用 repo 去抓商品
			return await _productTagRepo.GetProductsByTagIdsAsync(tagIds, take);
		}
	}
}
