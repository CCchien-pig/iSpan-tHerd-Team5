using tHerdBackend.Core.DTOs.SUP.Brand;

namespace tHerdBackend.Core.Interfaces.SUP
{
	public interface IBrandBannerService
	{
		/// <summary>
		// 依 FileId 取得單一 Banner DTO。
		/// </summary>
		Task<BannerDto?> GetByIdAsync(int fileId);

		/// <summary>
		/// 更新或插入一筆 Banner (資產檔案) 紀錄。
		/// </summary>
		/// <returns>最終的 FileId。</returns>
		Task<int> UpsertAsync(BannerDto dto, int reviserId);
	}
}
