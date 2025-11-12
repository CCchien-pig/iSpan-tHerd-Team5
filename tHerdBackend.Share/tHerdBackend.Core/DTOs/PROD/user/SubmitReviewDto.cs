namespace tHerdBackend.Core.DTOs.PROD.user
{
    public class SubmitReviewDto
    {
        public int ProductId { get; set; }
        public int? SkuId { get; set; }
        public int Rating { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
