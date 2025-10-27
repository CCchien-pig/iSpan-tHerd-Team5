namespace tHerdBackend.SUP.Rcl.Areas.SUP.ViewModels
{
	/// <summary>
	/// 品牌版面單一區塊的屬性內容 (對應 LayoutJson 內的 "props" 物件)
	/// 用來承載 JSON 中每個區塊的 props 內容
	/// </summary>
	public class BrandLayoutBlockPropsViewModel
	{
		// Banner/通用 屬性
		public string? Title { get; set; }
		public string? Subtitle { get; set; }
		public string? LinkUrl { get; set; }

		// 媒體庫 (SYS_AssetFile) 相關屬性
		public int? FileId { get; set; }
		public string? FileUrl { get; set; } // 方便 View 直接預覽
		public string? AltText { get; set; }

		// Accordion/Feature 屬性
		public string? Content { get; set; } // 支援 HTML 內容

		// 任何其他可能的屬性都應該在這裡定義，以供反序列化和表單使用
	}
}
