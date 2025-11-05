namespace tHerdBackend.Core.DTOs.SUP.Brand
{
	/// <summary>
	/// 品牌收藏清單項目
	/// </summary>
	public class BrandFavoriteItemDto
	{
		/// <summary>品牌 Id</summary>
		public int BrandId { get; set; }

		/// <summary>品牌名稱</summary>
		public string BrandName { get; set; } = "";

		/// <summary>建立時間</summary>
		public DateTime CreatedDate { get; set; }
	}
}
