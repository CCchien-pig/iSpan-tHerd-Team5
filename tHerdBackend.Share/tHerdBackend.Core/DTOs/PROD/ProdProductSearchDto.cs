namespace tHerdBackend.Core.DTOs.PROD
{
    /// <summary>
    /// 商品清單
    /// </summary>
    public class ProdProductSearchDto
	{
        public virtual int ProductId { get; set; } = 0;

        public virtual string ProductName { get; set; } = string.Empty;

        public virtual int BrandId { get; set; } = 0;

        public virtual string BrandName { get; set; } = string.Empty;

        public virtual int SeoId { get; set; } = 0;

        public virtual string Badge { get; set; } = string.Empty;

        public virtual string BadgeName { get; set; } = string.Empty;

        public virtual int MainSkuId { get; set; } = 0;

        public virtual string ImageUrl { get; set; } = string.Empty;

        /// <summary>
        /// 主商品原價
        /// </summary>
        public virtual decimal? ListPrice { get; set; }

        /// <summary>
        /// 主商品單價
        /// </summary>
        public virtual decimal? UnitPrice { get; set; }

        /// <summary>
        /// 主商品優惠價
        /// </summary>
        public virtual decimal? SalePrice { get; set; }

        /// <summary>
        /// 結帳單價
        /// </summary>
        private static decimal? FirstPositive(params decimal?[] prices)
    => prices.FirstOrDefault(p => p.HasValue && p.Value > 0);

        public decimal? BillingPrice => FirstPositive(SalePrice, UnitPrice, ListPrice);

        /// <summary>
        /// 評價數
        /// </summary>
        public virtual int? ReviewCount { get; set; }

        /// <summary>
        /// 星星平均
        /// </summary>
        public virtual int? AvgRating { get; set; }
    }
}
