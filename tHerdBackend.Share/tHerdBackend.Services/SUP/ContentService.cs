using Microsoft.Extensions.DependencyInjection;
using tHerdBackend.Core.DTOs.SUP.Brand;
using tHerdBackend.Core.Interfaces.SUP;

namespace tHerdBackend.Services.SUP
{
	public class ContentService : IContentService
	{
		private readonly IServiceProvider _serviceProvider; // 用於動態獲取 Repository

		public ContentService(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider;
		}

		// 輔助方法：透過 ServiceProvider 動態獲取正確的 Repository 實例
		private IGenericContentRepository<TDto> GetRepository<TDto>() where TDto : class
		{
			return _serviceProvider.GetRequiredService<IGenericContentRepository<TDto>>();
		}

		public Task<TDto?> GetContentByIdAsync<TDto>(int contentId) where TDto : class
		{
			return GetRepository<TDto>().GetByIdAsync(contentId);
		}

		/// <summary>
		/// 以交易方式 Upsert (更新或新增) 任何類型的內容。
		/// </summary>
		public async Task<int> UpsertContentAsync<TDto>(TDto dto, int brandId, int reviserId, int orderSeq)
			where TDto : class, IContentDto // 確保 TDto 實作了 IContentDto
		{
			// 將 Service 層的參數賦值給 DTO (如果 DTO 屬性存在)
			// 這裡可以使用反射或手動判斷，但最簡單的方式是在 Repository 層處理

			// 【核心修正點】根據 ContentId 判斷是更新還是新增
			if (dto.ContentId > 0)
			{
				// 更新 (Update)
				dto.Reviser = reviserId;
				// 根據 DTO 類型決定是否賦值 (更安全的做法)
				if (dto is BrandAccordionContentDto accordionDto) accordionDto.OrderSeq = orderSeq;
				if (dto is BrandArticleDto articleDto) articleDto.OrderSeq = orderSeq;
				// TODO: 其他 DTO 類型的處理

				await GetRepository<TDto>().UpdateAsync(dto);
				return dto.ContentId;
			}
			else
			{
				// 新增 (Create)
				dto.Creator = reviserId;
				// 根據 DTO 類型決定是否賦值
				if (dto is BrandAccordionContentDto accordionDto)
				{
					accordionDto.BrandId = brandId;
					accordionDto.OrderSeq = orderSeq;
				}
				if (dto is BrandArticleDto articleDto)
				{
					articleDto.BrandId = brandId;
					articleDto.OrderSeq = orderSeq;
				}

				return await GetRepository<TDto>().CreateAsync(dto);
			}
		}
	}
}
