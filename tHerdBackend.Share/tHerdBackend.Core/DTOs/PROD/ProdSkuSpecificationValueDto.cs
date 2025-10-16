namespace tHerdBackend.Core.DTOs.PROD
{
    /// <summary>
    /// 商品的規格設定
    /// </summary>
    public partial class ProdSkuSpecificationValueDto
    {
        /// <summary>
        /// Sku編號
        /// </summary>
        public int SkuId { get; set; }
        /// <summary>
        /// 規格值編號
        /// </summary>
        public int SpecificationOptionId { get; set; }
    }
}
