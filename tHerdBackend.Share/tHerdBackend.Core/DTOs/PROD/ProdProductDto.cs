using tHerdBackend.Core.Interfaces.PROD;
using tHerdBackend.Core.ValueObjects;

namespace tHerdBackend.Core.DTOs.PROD
{
    /// <summary>
    /// 商品清單
    /// </summary>
    public class ProdProductDto : IProductUserInfo
	{
		public string BrandName { get; set; } = string.Empty;

		public int ProductId { get; set; } = 0;

		public List<string> ProductTypeDesc { get; set; } = new List<string>();

		public string ProductCode { get; set; } = string.Empty;

		public string ProductName { get; set; } = string.Empty;

		public string SupplierName { get; set; } = string.Empty;

		public bool IsPublished { get; set; } = false;

        public int Creator { get; set; }

		public string CreatorNm { get; set; } = string.Empty;

		public DateTime CreatedDate { get; set; } = DateTime.MinValue;

		public string FormateCreatedDate => DateTimeHelper.ToDateTimeString(CreatedDate);

		public int? Reviser { get; set; }

		public string ReviserNm { get; set; } = string.Empty;

		/// <summary>
		/// 商品標籤
		/// </summary>
		public string Badge { get; set; } = string.Empty;

        /// <summary>
        /// 評價星數
        /// </summary>
        public decimal? AvgRating { get; set; }

        /// <summary>
        /// 評價總數
        /// </summary>
        public int? ReviewCount { get; set; }

        /// <summary>
        /// 商品主圖
        /// </summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// 主商品SkuId
        /// </summary>
        public int MainSkuId { get; set; }

        /// <summary>
        /// 主商品原價
        /// </summary>
        public decimal? ListPrice { get; set; }


        /// <summary>
        /// 主商品單價
        /// </summary>
        public decimal? UnitPrice { get; set; }

        /// <summary>
        /// 主商品優惠價
        /// </summary>
        public decimal? SalePrice { get; set; }
    }
}
