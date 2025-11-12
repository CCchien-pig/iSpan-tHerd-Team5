namespace tHerdBackend.Infra.Models.Sup
{
	/// <summary>
	/// 品牌詳頁 DTO：一次帶齊 Banner、分類按鈕、Accordion
	/// </summary>
	public class BrandDetailDto
	{
		public int BrandId { get; set; }
		public string BrandName { get; set; } = string.Empty;
		public string? BannerUrl { get; set; }

		public List<BrandButtonDto> Buttons { get; set; } = new();
		public List<BrandAccordionGroupDto> Accordions { get; set; } = new();

		// optional: 之後若有佈局設定可回傳 ['Banner','Buttons','Accordion']
		public List<string>? OrderedBlocks { get; set; }
	}

	public class BrandButtonDto
	{
		public int Id { get; set; }
		public string Text { get; set; } = string.Empty;
		public int Order { get; set; }
        public string Slug { get; set; } = "";
    }

	public class BrandAccordionGroupDto
	{
		public string ContentKey { get; set; } = string.Empty;
		public List<BrandAccordionItemDto> Items { get; set; } = new();
	}

	public class BrandAccordionItemDto
	{
		public string Title { get; set; } = string.Empty;
		public string Body { get; set; } = string.Empty;
		public int Order { get; set; }
	}
}
