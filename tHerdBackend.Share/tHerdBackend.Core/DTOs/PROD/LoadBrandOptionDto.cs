namespace tHerdBackend.Core.DTOs.PROD
{
    /// <summary>
    /// 品牌選項
    /// </summary>
    public partial class LoadBrandOptionDto
    {
        /// <summary>
        /// 品牌編號
        /// </summary>
        public int BrandId { get; set; }

        /// <summary>
        /// 品牌名稱
        /// </summary>
        public string BrandName { get; set; }

        /// <summary>
        /// 品牌簡碼，唯一，非空，英文
        /// </summary>
        public string BrandCode { get; set; }

        /// <summary>
        /// 關聯供應商
        /// </summary>
        public int? SupplierId { get; set; }

        /// <summary>
        /// 供應商名稱
        /// </summary>
        public string SupplierName { get; set; }
    }
}
