namespace tHerdBackend.Core.Interfaces.SUP
{
	public interface IContentDto
	{
		// 所有內容都有的欄位
		int ContentId { get; set; } // 內容的主鍵
		
		int? BrandId { get; set; }
		string ContentTitle { get; set; }
		string Content { get; set; }

		// 儲存邏輯需要的欄位
		int? Creator { get; set; }
		int? Reviser { get; set; }
		bool IsActive { get; set; }
	}
}
