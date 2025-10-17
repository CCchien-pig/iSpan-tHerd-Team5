using System.ComponentModel.DataAnnotations;

namespace tHerdBackend.Core.DTOs.PROD
{
    /// <summary>
    /// 商品的規格設定
    /// </summary>
    public partial class ProdSpecificationConfigDto
    {
        /// <summary>
        /// 規格群組ID（主鍵）
        /// </summary>
        [Display(Name = "規格群組編號")]
        public int SpecificationConfigId { get; set; }

        /// <summary>
        /// 商品ID（外鍵）
        /// </summary>
        [Required(ErrorMessage = "{0} 必填")]
        [Display(Name = "商品ID")]
        public int ProductId { get; set; }

        /// <summary>
        /// 規格群組名稱（例如：容量、口味、顏色）
        /// </summary>
        [Required(ErrorMessage = "{0} 必填")]
        [Display(Name = "規格群組名稱")]
        [StringLength(50, ErrorMessage = "{0} 長度不可超過 {1}")]
        public string GroupName { get; set; } = string.Empty;

        /// <summary>
        /// 顯示順序
        /// </summary>
        [Display(Name = "顯示順序")]
        [Range(0, 999, ErrorMessage = "{0} 必須大於等於 {1}")]
        public int OrderSeq { get; set; }

        /// <summary>
        /// 規格選項
        /// </summary>
        [Display(Name = "規格選項")]
        public List<ProdSpecificationOptionDto>? SpecOptions { get; set; }
    }
}
