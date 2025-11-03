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

		// 【核心修正點】新增 LinkUrl，使其可以被 JSON 骨架儲存和讀取
		public string? LinkUrl { get; set; }
	}
}
