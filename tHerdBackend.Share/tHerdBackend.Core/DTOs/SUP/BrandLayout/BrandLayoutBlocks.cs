using System.Text.Json.Serialization;

namespace tHerdBackend.Core.DTOs.SUP.BrandLayout
{
	// =======================================================
	// 1. 屬性 DTOs (Props)
	// =======================================================

	/// <summary>
	/// Banner 區塊的屬性資料傳輸物件 (對應 LayoutJson.props)
	/// </summary>
	public class BannerPropsDto
	{
		public string? Title { get; set; }
		public string? Subtitle { get; set; }
		public int? FileId { get; set; }
		public string? FileUrl { get; set; }
		public string? LinkUrl { get; set; }
		public string? AltText { get; set; }
		public string? Caption { get; set; }


		// ✅ 同時支援 isActive 與 imageIsActive（任一存在皆可映射）
		[JsonPropertyName("isActive")]
		public bool IsActive { get; set; }

		[JsonPropertyName("imageIsActive")]
		public bool ImageIsActive
		{
			get => IsActive;
			set => IsActive = value;
		}
	}

	// 專門用於 Accordion 區塊的 Props (範例)
	public class AccordionPropsDto
	{
		// 【新增】對應 SUP_BrandAccordionContent 的主鍵
		public int ContentId { get; set; }

		// 【新增】對應 SUP_BrandAccordionContent.ContentTitle
		public string ContentTitle { get; set; }
		public string? Content { get; set; }
		public int? ImgId { get; set; }

	}

	// =======================================================
	// 2. 區塊結構 DTOs
	// =======================================================

	/// <summary>
	/// LayoutJson 陣列中每個區塊的基底結構 (對應 LayoutJson 陣列中的單一物件)
	/// </summary>
	public class BaseLayoutBlockDto
	{
		public string Id { get; set; } = Guid.NewGuid().ToString();

		// 此欄位用於序列化和反序列化時識別具體類型（如 "banner", "accordion"）
		public string Type { get; set; } = string.Empty;

		// 為了讓 JSON 序列化器能夠處理動態屬性，我們使用 object 類型
		// 實際在程式中處理時，可能需要進行二次反序列化
		public object Props { get; set; } = new object();
	}

	// 如果需要強型別的區塊 DTO 來輔助開發，可以定義子類別
	/// <summary>
	/// Banner 區塊的強型別 DTO
	/// </summary>
	public class BannerBlockDto : BaseLayoutBlockDto
	{
		public BannerBlockDto()
		{
			Type = "banner";
			Props = new BannerPropsDto(); // 初始化為強型別的 Props
		}

		// [System.Text.Json.Serialization.JsonPropertyName("props")] 
		// public new BannerPropsDto Props { get => (BannerPropsDto)base.Props; set => base.Props = value; }
		// 註: 複雜的強型別映射在 System.Text.Json 中操作較複雜，通常建議用基底 DTO 搭配輔助函式。
	}
}
