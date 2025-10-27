namespace tHerdBackend.Core.DTOs.PROD
{
    /// <summary>
    /// 商品分類設定檔，可支援多階層架構
    /// </summary>
    public partial class ProdProductTypeDto
    {
        /// <summary>
        /// 商品ID（外鍵）
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// 分類ID（外鍵）
        /// </summary>
        public int ProductTypeId { get; set; }

        /// <summary>
        /// 是否為主分類（1=是）
        /// </summary>
        public bool IsPrimary { get; set; }
    }
}
