namespace tHerdBackend.Core.DTOs.PROD
{
    public class ProductReviewDto
    {
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public int? SkuId { get; set; }
        public int UserNumberId { get; set; }
        public string UserName { get; set; } = "";
        public byte Rating { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public int HelpfulCount { get; set; }
        public int UnhelpfulCount { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<ProductReviewImageDto>? Images { get; set; }
    }

    public class ProductReviewImageDto
    {
        public int ReviewImageId { get; set; }
        public int ReviewId { get; set; }
        public string? ImageUrl { get; set; }
        public int OrderSeq { get; set; }
    }
}
