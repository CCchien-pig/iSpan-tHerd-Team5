namespace tHerdBackend.Core.DTOs.SUP.BrandLayout
{
	/// <summary>
	/// 用於反序列化 JSON 骨架
	/// </summary>
	public class BrandLayoutSkeletonBlockDto
	{
		// 【確認】使用 string? 確保能處理 JSON 中缺失的欄位
		public string? Type { get; set; }
		public int ContentId { get; set; } // 假設 Banner, Article 也用 ContentId
	}
}
