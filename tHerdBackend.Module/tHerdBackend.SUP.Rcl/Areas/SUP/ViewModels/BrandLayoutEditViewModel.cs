using System.ComponentModel.DataAnnotations;

namespace tHerdBackend.SUP.Rcl.Areas.SUP.ViewModels
{
	/// <summary>
	/// 品牌版面編輯器主 ViewModel (用於 MVC View 渲染)
	/// 是 EditLayout Action 傳遞給主 Partial View 的資料模型
	/// </summary>
	public class BrandLayoutEditViewModel
	{
		public int BrandId { get; set; }

		[Display(Name = "品牌名稱")]
		public string BrandName { get; set; } = string.Empty;

		public int? ActiveLayoutId { get; set; } // 現行啟用版面的 LayoutId (若為新增則為 null)

		[Display(Name = "版型版本號")]
		public string? LayoutVersion { get; set; }

		/// <summary>
		/// 已反序列化的區塊列表 (供 Razor 迴圈渲染)
		/// </summary>
		public List<LayoutBlockViewModel> LayoutBlocks { get; set; } = new List<LayoutBlockViewModel>();

		// 接收前端提交的 JSON 字串（隱藏欄位）
		[Display(Name = "版面 JSON 內容")]
		[Required(ErrorMessage = "版面配置內容不能為空。")]
		public string LayoutJson { get; set; } = string.Empty;
	}
}
