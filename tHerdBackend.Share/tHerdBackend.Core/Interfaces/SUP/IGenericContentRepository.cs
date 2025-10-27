namespace tHerdBackend.Core.Interfaces.SUP
{
	// TEntity 為 EF Core Entity，TDto 為對應的 DTO
	// 泛型介面 通用內容倉儲
	public interface IGenericContentRepository<TDto>
		where TDto : class // 限制 TDto 必須是類別
	{
		/// <summary>
		/// 依 ID 取得單一 DTO。
		/// </summary>
		Task<TDto?> GetByIdAsync(int contentId);

		/// <summary>
		/// 建立新的內容 DTO。
		/// </summary>
		/// <returns>新建立的 ContentId/FileId。</returns>
		Task<int> CreateAsync(TDto dto);

		/// <summary>
		/// 更新現有的內容 DTO。
		/// </summary>
		Task UpdateAsync(TDto dto);
	}
}
