using System.ComponentModel.DataAnnotations;

namespace tHerdBackend.Core.DTOs.PROD
{
    /// <summary>
    /// 商品的規格設定的選項
    /// </summary>
    public partial class ProdSpecificationOptionDto
    {
        /// <summary>
        /// 規格選項ID（主鍵）
        /// </summary>
        [Display(Name = "規格選項ID")]
        public int? SpecificationOptionId { get; set; }

        /// <summary>
        /// 規格群組ID（外鍵）
        /// </summary>
        [Required(ErrorMessage = "{0} 必填")]
        [Display(Name = "規格群組ID")]
        public int? SpecificationConfigId { get; set; }

        /// <summary>
        /// 規格選項名稱（例如：250ml、巧克力）
        /// </summary>
        [Display(Name = "規格選項名稱")]
        [StringLength(50, ErrorMessage = "{0} 長度不可超過 {1}")]
        public string OptionName { get; set; }

        /// <summary>
        /// 顯示順序
        /// </summary>
        [Display(Name = "顯示順序")]
        [Range(0, 999, ErrorMessage = "{0} 必須大於等於 {1}")]
        public int OrderSeq { get; set; }

        /// <summary>
        /// 歸屬SKU編碼 (PROD_SkuSpecificationValue.SkuId)
        /// </summary>
        public int SkuId { get; set; }
    }
}
