using FlexBackend.Core.ValueObjects;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace FlexBackend.Core.DTOs.PROD;

/// <summary>
/// 商品基本資料
/// </summary>
public partial class ProdProductDto
{
	/// <summary>
	/// 商品ID
	/// </summary>
	[Display(Name = "商品編號")]
	public int ProductId { get; set; }

	/// <summary>
	/// 商品名稱
	/// </summary>
	[Display(Name = "商品名稱")]
	[Required(ErrorMessage = "{0}為必填")]
	public string ProductName { get; set; }

	/// <summary>
	/// 商品簡碼
	/// </summary>
	[Display(Name = "商品簡碼")]
	[Required(ErrorMessage = "{0}為必填")]
	public string ProductCode { get; set; }

	/// <summary>
	/// 供應商ID(FK)
	/// </summary>
	public int SupplierId { get; set; }

	/// <summary>
	/// 供應商名稱
	/// </summary>
	public string SupplierName { get; set; }

	/// <summary>
	/// 品牌ID
	/// </summary>
	[Display(Name = "品牌")]
	[Required(ErrorMessage = "請選擇{0}")]
	public int BrandId { get; set; }

	/// <summary>
	/// 品牌名稱
	/// </summary>
	public string BrandName { get; set; }

	/// <summary>
	/// Seo設定
	/// </summary>
	public int? SeoId { get; set; }

	/// <summary>
	/// 商品簡短描述，常用於列表展示
	/// </summary>
	[Display(Name = "商品簡述")]
	[Required(ErrorMessage = "{0}為必填")]
	public string ShortDesc { get; set; }

	/// <summary>
	/// 商品完整描述，用於詳細頁
	/// </summary>
	[Display(Name = "商品說明")]
	[Required(ErrorMessage = "{0}為必填")]
	public string FullDesc { get; set; }

    /// <summary>
    /// 是否上架（0=否，1=是）
    /// </summary>
    [Display(Name = "是否上架")]
    public bool IsPublished { get; set; }

	/// <summary>
	/// 重量（公斤）
	/// </summary>
	[Display(Name = "重量（公斤）")]
	[Required(ErrorMessage = "{0}為必填")]
	public decimal? Weight { get; set; }

	/// <summary>
	/// 體積
	/// </summary>
	[Display(Name = "體積")]
	[Required(ErrorMessage = "{0}為必填")]
	public decimal? VolumeCubicMeter { get; set; }

	/// <summary>
	/// 體積單位
	/// </summary>
	[Display(Name = "體積單位")]
	[Required(ErrorMessage = "請選擇{0}")]
	public string VolumeUnit { get; set; }

    /// <summary>
    /// 建檔人員
    /// </summary>
    [Display(Name = "建檔人員")]
    public int Creator { get; set; }

	/// <summary>
	/// 建檔姓名
	/// </summary>
	[BindNever]
	public string CreatorNm { get; set; }

	/// <summary>
	/// 建立時間
	/// </summary>
	[BindNever]
	public DateTime CreatedDate { get; set; }

	public string FormateCreatedDate => DateTimeHelper.ToDateTimeString(CreatedDate);

	/// <summary>
	/// 異動人員
	/// </summary>
	[BindNever]
    [Display(Name = "異動人員")]
    public int? Reviser { get; set; }

	/// <summary>
	/// 異動姓名
	/// </summary>
	[BindNever]
	public string ReviserNm { get; set; }

	/// <summary>
	/// 異動時間
	/// </summary>
	public DateTime? RevisedDate { get; set; }

	public string FormateRevisedDate => DateTimeHelper.ToDateTimeString(RevisedDate);

	/// <summary>
	/// 分類敘述
	/// </summary>
	public List<string> ProductTypeDesc { get; set; }

	public PRODSeoConfigDto? Seo { get; set; }

	public List<ProdProductSkuDto>? Skus { get; set; }
    public List<ProdSpecificationConfigDto> SpecConfigs { get; set; } = new();
}

/// <summary>
/// Sku 規格資料
/// </summary>
public partial class ProdProductSkuDto
{
    /// <summary>
    /// SKU ID（主鍵）
    /// </summary>
    [Display(Name = "SKU 編號")]
    public int SkuId { get; set; }

    /// <summary>
    /// 規格碼
    /// </summary>
    [Required(ErrorMessage = "{0} 必填")]
    [Display(Name = "規格碼")]
    [StringLength(50, ErrorMessage = "{0} 長度不可超過 {1}")]
    public string SpecCode { get; set; }

    /// <summary>
    /// SKU代碼
    /// </summary>
    public string SkuCode { get; set; }

    /// <summary>
    /// 商品ID（外鍵）
    /// </summary>
    [Display(Name = "商品ID")]
    public int ProductId { get; set; }

    /// <summary>
    /// 條碼
    /// </summary>
    [Display(Name = "條碼")]
    [StringLength(100, ErrorMessage = "{0} 長度不可超過 {1}")]
    public string Barcode { get; set; }

    /// <summary>
    /// 成本價
    /// </summary>
    [Display(Name = "成本價")]
    [Range(0, 9999999, ErrorMessage = "{0} 必須大於等於 {1}")]
    public decimal? CostPrice { get; set; }

    /// <summary>
    /// 原價
    /// </summary>
    [Display(Name = "原價")]
    [Range(0, 9999999, ErrorMessage = "{0} 必須大於等於 {1}")]
    public decimal? ListPrice { get; set; }

    /// <summary>
    /// 單價
    /// </summary>
    [Display(Name = "單價")]
    [Range(0, 9999999, ErrorMessage = "{0} 必須大於等於 {1}")]
    public decimal? UnitPrice { get; set; }

    /// <summary>
    /// 優惠價
    /// </summary>
    [Required(ErrorMessage = "{0} 必填")]
    [Display(Name = "優惠價")]
    [Range(0, 9999999, ErrorMessage = "{0} 必須大於等於 {1}")]
    public decimal SalePrice { get; set; }

    /// <summary>
    /// 目前庫存
    /// </summary>
    [Display(Name = "目前庫存")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} 必須大於等於 {1}")]
    public int StockQty { get; set; }

    /// <summary>
    /// 安全庫存量（低於提醒）
    /// </summary>
    [Display(Name = "安全庫存量")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} 必須大於等於 {1}")]
    public int SafetyStockQty { get; set; }

    /// <summary>
    /// 再訂購點（≧安全庫存量）
    /// </summary>
    [Display(Name = "再訂購點")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} 必須大於等於 {1}")]
    public int ReorderPoint { get; set; }

    /// <summary>
    /// 最大庫存量（0=不限制)
    /// </summary>
    [Display(Name = "最大庫存量")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} 必須大於等於 {1}")]
    public int MaxStockQty { get; set; }

    /// <summary>
    /// 是否允許缺貨預購（1=可超賣，0=禁止）
    /// </summary>
    [Display(Name = "允許缺貨預購")]
    public bool IsAllowBackorder { get; set; }

    /// <summary>
    /// 有效天數
    /// </summary>
    [Display(Name = "有效天數")]
    [Range(0, 36500, ErrorMessage = "{0} 必須介於 {1} 到 {2} 天")]
    public int ShelfLifeDays { get; set; }

    /// <summary>
    /// 上架開始時間
    /// </summary>
    [Required(ErrorMessage = "{0} 必填")]
    [Display(Name = "上架時間")]
    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = true)]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 下架時間（NULL=無限期）
    /// </summary>
    [Display(Name = "下架時間")]
    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// 是否啟用
    /// </summary>
    [Display(Name = "是否啟用")]
    public bool IsActive { get; set; }

    /// <summary>
    /// 規格選項
    /// </summary>
    [Display(Name = "規格選項")]
    public List<ProdSpecificationOptionDto>? SpecOptions { get; set; }
}

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
}

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
}

/// <summary>
/// 商品分類設定檔，可支援多階層架構
/// </summary>
public partial class ProdProductTypeConfig
{
	/// <summary>
	/// 分類ID（主鍵）
	/// </summary>
	public int ProductTypeId { get; set; }

	/// <summary>
	/// 父分類ID（NULL 代表最上層）
	/// </summary>
	public int? ParentId { get; set; }

	/// <summary>
	/// 分類簡碼
	/// </summary>
	public string ProductTypeCode { get; set; }

	/// <summary>
	/// 分類名稱
	/// </summary>
	public string ProductTypeName { get; set; }

	/// <summary>
	/// Seo 設定
	/// </summary>
	public int? SeoId { get; set; }

	/// <summary>
	/// 顯示順序
	/// </summary>
	public int OrderSeq { get; set; }

	/// <summary>
	/// 是否啟用分類（0=否，1=是）
	/// </summary>
	public bool IsActive { get; set; }
}

/// <summary>
///  SEO 設定
/// </summary>
public partial class PRODSeoConfigDto
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

/// <summary>
/// 商品分類設定檔，可支援多階層架構
/// </summary>
public partial class ProdProductTypeConfigDto
{
	/// <summary>
	/// 分類ID（主鍵）
	/// </summary>
	public int ProductTypeId { get; set; }

	/// <summary>
	/// 父分類ID（NULL 代表最上層）
	/// </summary>
	public int? ParentId { get; set; }

	/// <summary>
	/// 分類簡碼
	/// </summary>
	public string ProductTypeCode { get; set; }

	/// <summary>
	/// 分類名稱
	/// </summary>
	public string ProductTypeName { get; set; }

	/// <summary>
	/// Seo 設定
	/// </summary>
	public int? SeoId { get; set; }

	/// <summary>
	/// 顯示順序
	/// </summary>
	public int OrderSeq { get; set; }

	/// <summary>
	/// 是否啟用分類（0=否，1=是）
	/// </summary>
	public bool IsActive { get; set; }
}

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