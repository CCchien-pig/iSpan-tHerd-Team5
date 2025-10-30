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
		public int ImgId { get; set; }
		public bool IsMain { get; set; }
        public int OrderSeq { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string AltText { get; set; } = string.Empty;
		public string Caption { get; set; } = string.Empty;
		public int? Width { get; set; }
		public int? Height { get; set; }
        public string FileKey { get; set; } = string.Empty;
        public string FileExt { get; set; } = string.Empty;
        public bool IsExternal { get; set; } = false;
        public string MimeType { get; set; } = string.Empty;
        public long? FileSizeBytes { get; set; }
        public DateTime CreatedDate { get; set; }

	}
}
