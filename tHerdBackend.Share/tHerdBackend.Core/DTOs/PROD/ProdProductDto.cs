using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.Interfaces.PROD;
using tHerdBackend.Core.ValueObjects;

namespace tHerdBackend.Core.DTOs.PROD
{
    /// <summary>
    /// 商品清單
    /// </summary>
    public class ProdProductDto : IProductUserInfo
	{
		public virtual string BrandName { get; set; } = string.Empty;

		public virtual int ProductId { get; set; } = 0;

		public virtual List<string> ProductTypeDesc { get; set; } = new List<string>();

		public virtual string ProductCode { get; set; } = string.Empty;

		public virtual string ProductName { get; set; } = string.Empty;

		public virtual string SupplierName { get; set; } = string.Empty;

		public virtual bool IsPublished { get; set; } = false;

        public virtual int Creator { get; set; }

		public virtual string CreatorNm { get; set; } = string.Empty;

		public virtual DateTime CreatedDate { get; set; } = DateTime.MinValue;

		public virtual string FormateCreatedDate => DateTimeHelper.ToDateTimeString(CreatedDate);

		public virtual int? Reviser { get; set; }

		public virtual string ReviserNm { get; set; } = string.Empty;

		/// <summary>
		/// 商品標籤
		/// </summary>
		public virtual string Badge { get; set; } = string.Empty;

        /// <summary>
        /// 商品標籤名稱
        /// </summary>
        public virtual string BadgeName { get; set; } = string.Empty;

        /// <summary>
        /// 評價星數
        /// </summary>
        public virtual decimal? AvgRating { get; set; }

        /// <summary>
        /// 評價總數
        /// </summary>
        public virtual int? ReviewCount { get; set; }

        /// <summary>
        /// 商品主圖
        /// </summary>
        public virtual string? ImageUrl { get; set; }

        /// <summary>
        /// 主商品SkuId
        /// </summary>
        public virtual int MainSkuId { get; set; }

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

        public virtual string? ProductTypeName { get; set; }
    }
}
