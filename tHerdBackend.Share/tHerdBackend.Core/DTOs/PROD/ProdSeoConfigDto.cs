namespace tHerdBackend.Core.DTOs.PROD
{
    /// <summary>
    ///  SEO 設定
    /// </summary>
    public partial class ProdSeoConfigDto
    {
        /// <summary>
        /// SEO 編號
        /// </summary>
        public int SeoId { get; set; }

        /// <summary>
        /// 來源表名稱
        /// </summary>
        public string RefTable { get; set; }

        /// <summary>
        /// 來源 ID
        /// </summary>
        public int RefId { get; set; }

        /// <summary>
        /// 頁面唯一 URL 標識
        /// </summary>
        public string SeoSlug { get; set; }

        /// <summary>
        /// SEO 標題簡稱
        /// </summary>
        public string SeoTitle { get; set; }

        /// <summary>
        /// SEO 簡短描述
        /// </summary>
        public string SeoDesc { get; set; }

        /// <summary>
        /// 建檔時間
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// 異動時間
        /// </summary>
        public DateTime? RevisedDate { get; set; }
    }
}
