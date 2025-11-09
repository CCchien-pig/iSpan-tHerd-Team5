namespace tHerdBackend.Core.DTOs.PROD
{
	public partial class ProductFilterQueryDto
	{
		public int PageIndex { get; set; } = 1;     // 第幾頁
		public int PageSize { get; set; } = 20;     // 每頁筆數
		public string? Keyword { get; set; }        // 關鍵字
		public int? ProductTypeId { get; set; }     // 類別編號
		public int? BrandId { get; set; }           // 品牌編號
		public decimal? MinPrice { get; set; }      // 最低價
		public decimal? MaxPrice { get; set; }      // 最高價
		public string? SortBy { get; set; }         // 排序欄位
		public bool SortDesc { get; set; } = false; // 是否倒序
		public bool? IsPublished { get; set; }      // 是否發佈
		public bool? IsFrontEnd { get; set; }       // 是否來自前端

		public int? ProductId { get; set; }
	}

	public partial class ProductFrontFilterQueryDto
	{
		public int PageIndex { get; set; } = 1;     // 第幾頁
		public int PageSize { get; set; } = 20;     // 每頁筆數
		public string? Keyword { get; set; }        // 關鍵字
		public int? ProductTypeId { get; set; }     // 類別編號
		public int? BrandId { get; set; }           // 品牌編號

        // 價格區間
        public decimal? MinPrice { get; set; }      // 最低價
		public decimal? MaxPrice { get; set; }      // 最高價

        // 排序設定
        public string? SortBy { get; set; }         // 排序欄位
		public bool SortDesc { get; set; } = false; // 是否倒序

        // 顯示控制
        public bool? IsPublished { get; set; }      // 是否發佈
		public bool? IsFrontEnd { get; set; }       // 是否來自前端

        // 特殊標籤與條件
        public string? Badge { get; set; }          // 標籤代號
		public string? Other { get; set; }          // 熱銷 : Hot

        // 多品牌支援
        public List<int>? BrandIds { get; set; }     // 多品牌查詢

        // 額外查詢條件
        public List<int>? ProductIdList { get; set; }// 多商品查詢
        public int? ProductId { get; set; }

        // 新增：篩選區塊對應
        public List<double>? Rating { get; set; }         // 評分篩選
        public List<AttributeFilterDto>? AttributeFilters { get; set; } // 屬性篩選
    }
}
