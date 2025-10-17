namespace tHerdBackend.Core.DTOs.PROD
{
    /// <summary>
    /// 商品照片資料
    /// </summary>
    public class ProductImageDto
    {
        public int ImageId { get; set; }
        public int ProductId { get; set; }
        public int? SkuId { get; set; }
        public bool IsMain { get; set; }
        public int OrderSeq { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string? AltText { get; set; }
    }
}
