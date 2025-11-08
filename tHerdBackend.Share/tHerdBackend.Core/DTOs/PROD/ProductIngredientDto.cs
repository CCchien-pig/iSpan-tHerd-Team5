namespace tHerdBackend.Core.DTOs.PROD
{
    /// <summary>
    /// 商品成分明細 DTO
    /// </summary>
    public class ProductIngredientDto
    {
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string? Alias { get; set; }
        public string? Description { get; set; }
        public decimal? Percentage { get; set; }
        public string? Note { get; set; }
    }
}
