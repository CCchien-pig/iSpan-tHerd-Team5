namespace tHerdBackend.Core.DTOs.PROD
{
    public class ProductTypeTreeDto
    {
        public int ProductTypeId { get; set; }
        public int? ParentId { get; set; }
        public string ProductTypeCode { get; set; } = string.Empty;
        public string ProductTypeName { get; set; } = string.Empty;
        public int OrderSeq { get; set; }
        public bool IsActive { get; set; }

        public List<ProductTypeTreeDto>? Children { get; set; }
    }
}
