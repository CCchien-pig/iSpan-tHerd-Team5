namespace tHerdBackend.Core.Interfaces.PROD
{
	// 共用介面：讓兩個 DTO 都能共用 Apply()
	public interface IProductUserInfo
	{
		int Creator { get; set; }
		int? Reviser { get; set; }
		string? CreatorNm { get; set; }
		string? ReviserNm { get; set; }
	}
}
