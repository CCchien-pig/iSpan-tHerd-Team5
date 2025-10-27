namespace tHerdBackend.Core.Interfaces.SUP
{
	public interface IBrandContentService
	{
		// 通用的 Upsert 方法，透過 DTO 類型來識別內容模組
		Task<int> UpsertContentAsync<TDto>(
			TDto dto,
			int brandId,
			int reviserId,
			int orderSeq)
			where TDto : class, IContentDto;

		// 通用的 GetById 方法
		Task<TDto?> GetContentByIdAsync<TDto>(int contentId)
			where TDto : class, IContentDto;
	}
}
