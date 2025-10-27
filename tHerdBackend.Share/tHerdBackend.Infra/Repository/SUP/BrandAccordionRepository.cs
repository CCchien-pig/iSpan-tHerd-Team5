using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.SUP.Brand;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Infra.Models;

public class BrandAccordionRepository : IGenericContentRepository<BrandAccordionContentDto>
{
	private readonly tHerdDBContext _context;

	public BrandAccordionRepository(tHerdDBContext context) { _context = context; }

	// -----------------------------------------------------------------------
	// 1. 讀取 (Read)
	// -----------------------------------------------------------------------

	public async Task<BrandAccordionContentDto?> GetByIdAsync(int contentId)
	{
		// 【優化】使用 LINQ 投影（Projection）避免載入所有 Entity 欄位
		return await _context.SupBrandAccordionContents.AsNoTracking()
			.Where(e => e.ContentId == contentId)
			.Select(e => new BrandAccordionContentDto
			{
				ContentId = e.ContentId,
				BrandId = e.BrandId,
				ContentTitle = e.ContentTitle,
				Content = e.Content,
				OrderSeq = e.OrderSeq,
				ImgId = e.ImgId,
				IsActive = e.IsActive,
				Creator = e.Creator,
				CreatedDate = e.CreatedDate,
				Reviser = e.Reviser,
				RevisedDate = e.RevisedDate
			})
			.FirstOrDefaultAsync();
	}

	// -----------------------------------------------------------------------
	// 2. 建立 (Create)
	// -----------------------------------------------------------------------

	public async Task<int> CreateAsync(BrandAccordionContentDto dto)
	{
		// 【映射 DTO 到 Entity】
		var entity = new SupBrandAccordionContent
		{
			BrandId = dto.BrandId,
			ContentTitle = dto.ContentTitle,
			Content = dto.Content,
			OrderSeq = dto.OrderSeq,
			ImgId = dto.ImgId,
			IsActive = dto.IsActive,
			// 由於 DTO 是由 Service 層提供 Creator/Reviser ID，這裡直接使用
			Creator = dto.Creator,
			CreatedDate = DateTime.Now
		};

		_context.SupBrandAccordionContents.Add(entity);
		await _context.SaveChangesAsync();
		return entity.ContentId;
	}

	// -----------------------------------------------------------------------
	// 3. 更新 (Update)
	// -----------------------------------------------------------------------

	public async Task UpdateAsync(BrandAccordionContentDto dto)
	{
		// 必須先載入 Entity
		var entity = await _context.SupBrandAccordionContents.FindAsync(dto.ContentId);

		if (entity != null)
		{
			// 【映射 DTO 到 Entity】
			entity.ContentTitle = dto.ContentTitle;
			entity.Content = dto.Content;
			entity.OrderSeq = dto.OrderSeq;
			entity.ImgId = dto.ImgId;
			entity.IsActive = dto.IsActive;
			entity.Reviser = dto.Reviser;
			entity.RevisedDate = DateTime.Now;

			// EF Core 追蹤變更並儲存
			await _context.SaveChangesAsync();
		}
	}
}