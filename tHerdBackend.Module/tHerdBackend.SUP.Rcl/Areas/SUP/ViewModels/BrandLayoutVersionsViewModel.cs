using tHerdBackend.Core.DTOs.SUP;

namespace tHerdBackend.SUP.Rcl.Areas.SUP.ViewModels
{
	public class BrandLayoutVersionsViewModel
	{
		public int BrandId { get; set; }

		public string BrandName { get; set; } = string.Empty;

		/// <summary>
		/// 包含該品牌所有歷史 Layout 版本的簡要資訊列表
		/// </summary>
		// 這裡可以使用上面定義的 BrandLayoutVersionDto 或重新定義 View 層的 DTO
		public List<BrandLayoutVersionDto> Layouts { get; set; } = new List<BrandLayoutVersionDto>();
	}
}
