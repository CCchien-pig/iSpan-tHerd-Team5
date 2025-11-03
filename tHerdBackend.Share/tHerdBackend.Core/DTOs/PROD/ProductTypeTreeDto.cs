namespace tHerdBackend.Core.DTOs.PROD
{
    /// <summary>
    /// 商品分類樹狀結構 DTO（可用於前台分類樹）
    /// </summary>
    public class ProductTypeTreeDto
    {
        public int ProductTypeId { get; set; }
        public int? ParentId { get; set; }
        public string ProductTypeCode { get; set; } = string.Empty;
        public string ProductTypeName { get; set; } = string.Empty;
        public int OrderSeq { get; set; }
        public bool IsActive { get; set; }

        /// <summary>
        /// 子分類列表
        /// </summary>
        public List<ProductTypeTreeNodeDto> Children { get; set; } = new();

        /// <summary>
        /// 自動生成的分類 URL，例如 /products/vitamins-2040
        /// </summary>
        public string Url => $"/products/{ProductTypeCode.ToLower()}-{ProductTypeId}";
    }

    /// <summary>
    /// 子分類節點（Swagger 避免自我參照）
    /// </summary>
    public class ProductTypeTreeNodeDto
    {
        public int ProductTypeId { get; set; }
        public int? ParentId { get; set; }
        public string ProductTypeCode { get; set; } = string.Empty;
        public string ProductTypeName { get; set; } = string.Empty;
        public int OrderSeq { get; set; }
        public bool IsActive { get; set; }

        public List<ProductTypeTreeNodeDto>? Children { get; set; }

        /// <summary>
        /// 自動生成的分類 URL，例如 /products/vitamins-2040
        /// </summary>
        public string Url => $"/products/{ProductTypeCode.ToLower()}-{ProductTypeId}";
    }
}