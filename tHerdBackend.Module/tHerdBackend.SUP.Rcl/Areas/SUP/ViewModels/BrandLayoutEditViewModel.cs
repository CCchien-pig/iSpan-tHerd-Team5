using System.ComponentModel.DataAnnotations;

namespace tHerdBackend.SUP.Rcl.Areas.SUP.ViewModels
{
	/// <summary>
	/// 品牌版面編輯器主 ViewModel (用於 MVC View 渲染)
	/// 是 EditLayout Action 傳遞給主 Partial View 的資料模型
	/// </summary>
	public class BrandLayoutEditViewModel
	{
		[Display(Name = "版本Id")]
		public int? LayoutId { get; set; } // 如果是新增，則為 null

		[Display(Name = "品牌Id")]
		public int BrandId { get; set; }

		[Display(Name = "品牌名稱")]
		public string BrandName { get; set; } = string.Empty;

		[Display(Name = "現行啟用版本Id")]
		public int? CurrentActiveLayoutId { get; set; } // 現行啟用版面的 LayoutId (若為新增則為 null)

		[Display(Name = "版型版本號")]
		public string? LayoutVersion { get; set; }

		/// <summary>
		/// 已反序列化的區塊列表 (供 Razor 迴圈渲染)，Vue 編輯器的核心 Model，包含所有區塊的陣列
		/// </summary>
		public List<BrandLayoutBlockViewModel> LayoutBlocks { get; set; } = new List<BrandLayoutBlockViewModel>();

		// 接收前端提交的 JSON 字串（隱藏欄位）
		[Display(Name = "版面 JSON 內容")]
		[Required(ErrorMessage = "版面配置內容不能為空。")]
		public string LayoutJson { get; set; } = string.Empty;

		// 用來驗證版本號不重複
		public List<string> AllLayoutVersions { get; set; }
	}
}
