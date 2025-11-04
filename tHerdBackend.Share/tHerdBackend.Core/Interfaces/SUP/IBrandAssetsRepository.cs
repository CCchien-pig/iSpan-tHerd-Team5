namespace tHerdBackend.Core.Interfaces.SUP
{
	/// <summary>
	/// 專責「一般品牌資產檔」的查詢封裝（像 FolderId=8 的內容圖片），輸出簡單型別（List<string> urls）或對應 DTO。
	/// </summary>
	public interface IBrandAssetsRepository
	{
		Task<List<string>> GetContentImagesAsync(int brandId, int folderId, string? altText, CancellationToken ct);
	}
}
