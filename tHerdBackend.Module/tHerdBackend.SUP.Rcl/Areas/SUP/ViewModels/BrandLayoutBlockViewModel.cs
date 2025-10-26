namespace tHerdBackend.SUP.Rcl.Areas.SUP.ViewModels
{
	/// <summary>
	/// 品牌版面單一區塊的結構 (對應 LayoutJson 陣列中的單一物件)
	/// 用來承載 JSON 陣列中的單一元素
	/// </summary>
	public class BrandLayoutBlockViewModel
	{
		/// <summary>
		/// 區塊的唯一識別碼 (對應 JSON "id")
		/// </summary>
		public string Id { get; set; } = Guid.NewGuid().ToString();

		/// <summary>
		/// 區塊類型 (對應 JSON "type", 例如: "banner", "accordion")
		/// </summary>
		public string Type { get; set; } = string.Empty;

		/// <summary>
		/// 區塊屬性 (對應 JSON "props") - 改回 object 以匹配 BaseLayoutBlockDto
		/// Vue 會在前端處理其內容，C# 只負責傳遞。
		/// </summary>
		public object Props { get; set; } = new object();
	}
}
