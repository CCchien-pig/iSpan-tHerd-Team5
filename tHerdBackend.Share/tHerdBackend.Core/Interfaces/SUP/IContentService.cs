namespace tHerdBackend.Core.Interfaces.SUP
{
	// 通用的內容服務
	public interface IContentService
	{
		/// <summary>
		/// 【泛型化】依據 ContentId 取得任何類型的內容 DTO。
		/// </summary>
		Task<TDto?> GetContentByIdAsync<TDto>(int contentId) where TDto : class;

		/// <summary>
		/// 【泛型化】以交易方式 Upsert (更新或新增) 任何類型的內容。
		/// </summary>
		Task<int> UpsertContentAsync<TDto>(
			TDto dto,
			int brandId,
			int reviserId,
			int orderSeq)
			where TDto : class, IContentDto;
	}
}
