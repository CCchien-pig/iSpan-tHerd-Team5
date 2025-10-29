using tHerdBackend.Core.Interfaces.SUP;

namespace tHerdBackend.Core.DTOs.SUP.Brand
{
	/// <summary>
	/// 用於傳遞 Banner (資產檔案) 數據的資料傳輸物件。
	/// </summary>
	public class BannerDto : IContentDto
	{
		// 【核心修正】將 FileId 映射到 ContentId
		public int ContentId { get => FileId; set => FileId = value; } // 實作 IContentDto.ContentId
		public int FileId { get; set; } // 真正的欄位
		public bool IsExternal { get; set; }
		public string FileUrl { get; set; }
		public string AltText { get; set; }
		public string Caption { get; set; }
		public bool IsActive { get; set; }

		// 【核心修正點】在這裡新增 LinkUrl 屬性
		public string? LinkUrl { get; set; }

		// 用於 Upsert 邏輯
		public int? Creator { get; set; }
		public int? Reviser { get; set; }

		public int? BrandId { get; set; }
		public string ContentTitle { get; set; }
		public string Content { get; set; }
		public string FileKey { get; set; }
	}
}
