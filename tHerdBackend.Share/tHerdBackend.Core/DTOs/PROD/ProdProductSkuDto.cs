using System.ComponentModel.DataAnnotations;

namespace tHerdBackend.Core.DTOs.PROD
{
    /// <summary>
    /// Sku 規格資料
    /// </summary>
    public partial class ProdProductSkuDto
    {
        /// <summary>
        /// SKU ID（主鍵）
        /// </summary>
        [Display(Name = "SKU 編號")]
        public int SkuId { get; set; }

        /// <summary>
        /// 規格碼
        /// </summary>
        [Required(ErrorMessage = "{0} 必填")]
        [Display(Name = "規格碼")]
        [StringLength(50, ErrorMessage = "{0} 長度不可超過 {1}")]
        public string SpecCode { get; set; }

        /// <summary>
        /// SKU代碼
        /// </summary>
        public string SkuCode { get; set; } = string.Empty;

        /// <summary>
        /// 商品ID（外鍵）
        /// </summary>
        [Display(Name = "商品ID")]
        public int ProductId { get; set; }

        /// <summary>
        /// 條碼
        /// </summary>
        [Display(Name = "條碼")]
        [StringLength(100, ErrorMessage = "{0} 長度不可超過 {1}")]
        [DisplayFormat(ConvertEmptyStringToNull = true)]
        public string? Barcode { get; set; }

        /// <summary>
        /// 成本價
        /// </summary>
        [Display(Name = "成本價")]
        [Required(ErrorMessage = "{0} 必填")]
        [Range(0, 9999999, ErrorMessage = "{0} 必須大於等於 {1}")]
        public decimal? CostPrice { get; set; } = 0;

        /// <summary>
        /// 原價
        /// </summary>
        [Display(Name = "原價")]
        [Required(ErrorMessage = "{0} 必填")]
        [Range(0, 9999999, ErrorMessage = "{0} 必須大於等於 {1}")]
        public decimal? ListPrice { get; set; } = 0;

        /// <summary>
        /// 單價
        /// </summary>
        [Display(Name = "單價")]
        [Required(ErrorMessage = "{0} 必填")]
        [Range(0, 9999999, ErrorMessage = "{0} 必須大於等於 {1}")]
        public decimal? UnitPrice { get; set; } = 0;

        /// <summary>
        /// 優惠價
        /// </summary>
        [Required(ErrorMessage = "{0} 必填")]
        [Display(Name = "優惠價")]
        [Range(0, 9999999, ErrorMessage = "{0} 必須大於等於 {1}")]
        public decimal? SalePrice { get; set; } = 0;

        /// <summary>
        /// 目前庫存
        /// </summary>
        [Display(Name = "目前庫存")]
        [DisplayFormat(ConvertEmptyStringToNull = true)]
        public int StockQty { get; set; } = 0;

        /// <summary>
        /// 安全庫存量（低於提醒）
        /// </summary>
        [Display(Name = "安全庫存量")]
        [Range(0, int.MaxValue, ErrorMessage = "{0} 必須大於等於 {1}")]
        public int SafetyStockQty { get; set; } = 0;

        /// <summary>
        /// 再訂購點（≧安全庫存量）
        /// </summary>
        [Display(Name = "再訂購點")]
        [Range(0, int.MaxValue, ErrorMessage = "{0} 必須大於等於 {1}")]
        public int ReorderPoint { get; set; } = 0;

        /// <summary>
        /// 最大庫存量（0=不限制)
        /// </summary>
        [Display(Name = "最大庫存量")]
        [Range(0, int.MaxValue, ErrorMessage = "{0} 必須大於等於 {1}")]
        public int MaxStockQty { get; set; } = 0;

        /// <summary>
        /// 是否允許缺貨預購（1=可超賣，0=禁止）
        /// </summary>
        [Display(Name = "允許缺貨預購")]
        public bool IsAllowBackorder { get; set; }

        /// <summary>
        /// 有效天數
        /// </summary>
        [Display(Name = "有效天數")]
        [Range(0, 36500, ErrorMessage = "{0} 必須介於 {1} 到 {2} 天")]
        public int ShelfLifeDays { get; set; } = 0;

        /// <summary>
        /// 上架開始時間
        /// </summary>
        [Required(ErrorMessage = "{0} 必填")]
        [Display(Name = "上架時間")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// 下架時間（NULL=無限期）
        /// </summary>
        [Display(Name = "下架時間")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// 是否啟用
        /// </summary>
        [Display(Name = "是否啟用")]
        public bool IsActive { get; set; }

        /// <summary>
        /// 規格對應的規格值清單
        /// </summary>
        public List<ProdSkuSpecificationValueDto>? SpecValues { get; set; }

        /// <summary>
        /// 軟刪除標記（1=已刪除）
        /// </summary>
        public bool IsDeleted { get; set; } = false;

		/// <summary>
		/// 結帳單價
		/// </summary>
		private static decimal? FirstPositive(params decimal?[] prices)
	=> prices.FirstOrDefault(p => p.HasValue && p.Value > 0);

		public decimal? BillingPrice => FirstPositive(SalePrice, UnitPrice, ListPrice);

		/// <summary>
		/// 商品規格統稱
		/// </summary>
		public string? OptionName { get; set; }

		/// <summary>
		/// 此 SKU 的所有效期清單（由外部填入，非資料庫欄位）
		/// </summary>
		public List<DateTime>? ExpiryDates { get; set; }

		/// <summary>
		/// 是否有庫存（依 StockQty > 0）
		/// </summary>
		[Display(Name = "是否有庫存")]
		public bool HasStock => StockQty > 0;

		/// <summary>
		/// 此 SKU 最舊效期（排除過期項目）
		/// </summary>
		[Display(Name = "最舊效期")]
		public DateTime? EarliestExpiryDate
		{
			get
			{
				if (ExpiryDates == null || !ExpiryDates.Any())
					return null;

				// 排除已過期項目，只取有效的最舊日期
				var now = DateTime.Now;
				return ExpiryDates
					.Where(d => d > now)
					.OrderBy(d => d)
					.FirstOrDefault();
			}
		}
	}
}
