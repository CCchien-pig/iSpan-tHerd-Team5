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

	public PRODSeoConfigDto Seo { get; set; }
	public ProdProductSkuDto Sku { get; set; }

	public IEnumerable<ProdSpecificationConfigDto> SpecConfig { get; set; }
}

/// <summary>
/// Sku 規格資料
/// </summary>
public partial class ProdProductSkuDto
{
	/// <summary>
	/// SKU ID（主鍵）
	/// </summary>
	public int SkuId { get; set; }

	/// <summary>
	/// 規格碼
	/// </summary>
	public string SpecCode { get; set; }

	/// <summary>
	/// SKU代碼
	/// </summary>
	public string SkuCode { get; set; }

	/// <summary>
	/// 商品ID（外鍵）
	/// </summary>
	public int ProductId { get; set; }

	/// <summary>
	/// 條碼
	/// </summary>
	public string Barcode { get; set; }

	/// <summary>
	/// 成本價
	/// </summary>
	public decimal? CostPrice { get; set; }

	/// <summary>
	/// 原價
	/// </summary>
	public decimal? ListPrice { get; set; }

	/// <summary>
	/// 單價
	/// </summary>
	public decimal? UnitPrice { get; set; }

	/// <summary>
	/// 優惠價
	/// </summary>
	public decimal SalePrice { get; set; }

	/// <summary>
	/// 目前庫存
	/// </summary>
	public int StockQty { get; set; }

	/// <summary>
	/// 安全庫存量（低於提醒）
	/// </summary>
	public int SafetyStockQty { get; set; }

	/// <summary>
	/// 再訂購點（≧安全庫存量）
	/// </summary>
	public int ReorderPoint { get; set; }

	/// <summary>
	/// 最大庫存量（0=不限制)
	/// </summary>
	public int MaxStockQty { get; set; }

	/// <summary>
	/// 是否允許缺貨預購（1=可超賣，0=禁止）
	/// </summary>
	public bool IsAllowBackorder { get; set; }

	/// <summary>
	/// 有效天數
	/// </summary>
	public int ShelfLifeDays { get; set; }

	/// <summary>
	/// 上架開始時間
	/// </summary>
	public DateTime StartDate { get; set; }

	/// <summary>
	/// 下架時間（NULL=無限期）
	/// </summary>
	public DateTime? EndDate { get; set; }

	/// <summary>
	/// 是否啟用
	/// </summary>
	public bool IsActive { get; set; }

	/// <summary>
	/// 規格選項
	/// </summary>
	public List<ProdSpecificationOptionDto> SpecOption { get; set; }

	/// <summary>
	/// 規格選項值
	/// </summary>
	public List<ProdSkuSpecificationValueDto> SpecValue { get; set; }
}

/// <summary>
/// 商品的規格設定
/// </summary>
public partial class ProdSpecificationConfigDto
{
	/// <summary>
	/// 規格群組ID（主鍵）
	/// </summary>
	public int SpecificationConfigId { get; set; }

	/// <summary>
	/// 商品ID（外鍵）
	/// </summary>
	public int ProductId { get; set; }

	/// <summary>
	/// 規格群組名稱（例如：容量、口味、顏色）
	/// </summary>
	public string GroupName { get; set; }

	/// <summary>
	/// 顯示順序
	/// </summary>
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
/// 商品的規格設定的選項
/// </summary>
public partial class ProdSpecificationOptionDto
{
	/// <summary>
	/// 規格選項ID（主鍵）
	/// </summary>
	public int SpecificationOptionId { get; set; }

	/// <summary>
	/// 規格群組ID（外鍵）
	/// </summary>
	public int SpecificationConfigId { get; set; }

	/// <summary>
	/// 規格選項名稱（例如：250ml、巧克力）
	/// </summary>
	public string OptionName { get; set; }

	/// <summary>
	/// 顯示順序
	/// </summary>
	public int OrderSeq { get; set; }
}

/// <summary>
/// SKU 與多個規格選項的對應
/// </summary>
public partial class ProdSkuSpecificationValueDto
{
	/// <summary>
	/// SKU ID（外鍵）
	/// </summary>
	public int SkuId { get; set; }

	/// <summary>
	/// 規格選項ID（外鍵）
	/// </summary>
	public int SpecificationOptionId { get; set; }

	/// <summary>
	/// 建立時間
	/// </summary>
	public DateTime? CreatedDate { get; set; }
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