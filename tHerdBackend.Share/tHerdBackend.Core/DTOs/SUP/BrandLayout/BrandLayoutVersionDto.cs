namespace tHerdBackend.Core.DTOs.SUP.BrandLayout
{
	public class BrandLayoutVersionDto
	{
		public int LayoutId { get; set; }
		public string BrandName { get; set; } = string.Empty; // 可選，用於確認
		public string? LayoutVersion { get; set; }
		public bool IsActive { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime? RevisedDate { get; set; }

		public int? Reviser { get; set; }
		// 注意：不包含 LayoutJson 欄位
	}
}
