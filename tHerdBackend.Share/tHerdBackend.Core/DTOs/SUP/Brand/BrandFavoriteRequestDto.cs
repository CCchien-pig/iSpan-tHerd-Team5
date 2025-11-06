namespace tHerdBackend.Core.DTOs.SUP.Brand
{
	/// <summary>
	/// 新增/取消品牌收藏請求
	/// </summary>
	public class BrandFavoriteRequestDto
	{
		/// <summary>會員 UserNumberId</summary>
		public int UserNumberId { get; set; }
		/// <summary>品牌 Id</summary>
		public int BrandId { get; set; }
	}
}
