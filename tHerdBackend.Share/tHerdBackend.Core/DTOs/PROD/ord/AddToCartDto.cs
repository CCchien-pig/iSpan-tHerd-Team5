namespace tHerdBackend.Core.DTOs.PROD.ord
{
    public class AddToCartDto
    {
        public int? UserNumberId { get; set; }
        public int SkuId { get; set; }
        public int Qty { get; set; } = 1;
        public decimal UnitPrice { get; set; }
    }
}
