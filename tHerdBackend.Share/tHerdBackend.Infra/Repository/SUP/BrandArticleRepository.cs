using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.SUP.Brand;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Infra.Repository.SUP
{
	public class BrandArticleRepository : IGenericContentRepository<BrandArticleDto>
	{
		private readonly tHerdDBContext _context;

		// 注入 Context
		public BrandArticleRepository(tHerdDBContext context)
		{
			_context = context;
		}

		// -----------------------------------------------------------------------
		// 1. 讀取 (Read)
		// -----------------------------------------------------------------------

		public async Task<BrandArticleDto?> GetByIdAsync(int contentId)
		{
			// 【映射 Entity 到 DTO】
			return await _context.SupBrandArticles.AsNoTracking()
				.Where(e => e.ContentId == contentId)
				.Select(e => new BrandArticleDto
				{
					ContentId = e.ContentId,
					BrandId = e.BrandId,
					ImgId = e.ImgId,
					ContentTitle = e.ContentTitle,
					Content = e.Content,
					ContentType = e.ContentType,
					OrderSeq = e.OrderSeq,
					IsActive = e.IsActive,
					PublishDate = e.PublishDate,
					Creator = e.Creator,
					Reviser = e.Reviser
					// 這裡只映射 DTO 中已定義的欄位
				})
				.FirstOrDefaultAsync();
		}

		// -----------------------------------------------------------------------
		// 2. 建立 (Create)
		// -----------------------------------------------------------------------

		public async Task<int> CreateAsync(BrandArticleDto dto)
		{
			// 【映射 DTO 到 Entity】
			var entity = new SupBrandArticle
			{
				BrandId = dto.BrandId,
				ImgId = dto.ImgId,
				ContentTitle = dto.ContentTitle,
				Content = dto.Content,
				ContentType = dto.ContentType,
				OrderSeq = dto.OrderSeq,
				IsActive = dto.IsActive,
				PublishDate = dto.PublishDate,
				Creator = dto.Creator,
				CreatedDate = DateTime.Now
			};

			_context.SupBrandArticles.Add(entity);
			await _context.SaveChangesAsync();
			return entity.ContentId;
		}

		// -----------------------------------------------------------------------
		// 3. 更新 (Update)
		// -----------------------------------------------------------------------

		public async Task UpdateAsync(BrandArticleDto dto)
		{
			// 必須先載入 Entity
			var entity = await _context.SupBrandArticles.FindAsync(dto.ContentId);

			if (entity != null)
			{
				// 【映射 DTO 到 Entity】
				entity.ImgId = dto.ImgId;
				entity.ContentTitle = dto.ContentTitle;
				entity.Content = dto.Content;
				entity.ContentType = dto.ContentType;
				entity.OrderSeq = dto.OrderSeq;
				entity.IsActive = dto.IsActive;
				entity.PublishDate = dto.PublishDate;

				// 追蹤欄位
				entity.Reviser = dto.Reviser;
				entity.RevisedDate = DateTime.Now;

				// EF Core 追蹤變更並儲存
				await _context.SaveChangesAsync();
			}
		}
	}
}
