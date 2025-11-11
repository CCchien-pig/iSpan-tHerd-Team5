using tHerdBackend.Core.ValueObjects;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace tHerdBackend.Core.DTOs.PROD;

/// <summary>
/// 商品基本資料 (詳細)
/// </summary>
public partial class ProdProductDetailDto : ProdProductDto
{
	/// <summary>
	/// 商品ID
	/// </summary>
	[Display(Name = "商品編號")]
	public override int ProductId { get; set; }

	/// <summary>
	/// 商品名稱
	/// </summary>
	[Display(Name = "商品名稱")]
	[Required(ErrorMessage = "{0}為必填")]
	public override string ProductName { get; set; }

	/// <summary>
	/// 商品簡碼
	/// </summary>
	[Display(Name = "商品簡碼")]
	[Required(ErrorMessage = "{0}為必填")]
	public override string ProductCode { get; set; }

	/// <summary>
	/// 供應商ID(FK)
	/// </summary>
	public int SupplierId { get; set; }

	/// <summary>
	/// 供應商名稱
	/// </summary>
	public override string SupplierName { get; set; }

	/// <summary>
	/// 品牌ID
	/// </summary>
	[Display(Name = "品牌")]
	[Required(ErrorMessage = "請選擇{0}")]
	public int BrandId { get; set; }

	/// <summary>
	/// 品牌名稱
	/// </summary>
	public override string BrandName { get; set; }

    /// <summary>
    /// 品牌簡碼，唯一，非空，英文
    /// </summary>
    public string BrandCode { get; set; }

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
    public override bool IsPublished { get; set; }

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
    public override int Creator { get; set; }

	/// <summary>
	/// 建檔姓名
	/// </summary>
	[BindNever]
	public override string? CreatorNm { get; set; }

	/// <summary>
	/// 建立時間
	/// </summary>
	[BindNever]
	public override DateTime CreatedDate { get; set; }

	public override string FormateCreatedDate => DateTimeHelper.ToDateTimeString(CreatedDate);

	/// <summary>
	/// 異動人員
	/// </summary>
	[BindNever]
    [Display(Name = "異動人員")]
    public override int? Reviser { get; set; }

	/// <summary>
	/// 異動姓名
	/// </summary>
	[BindNever]
	public override string ReviserNm { get; set; }

	/// <summary>
	/// 異動時間
	/// </summary>
	public DateTime? RevisedDate { get; set; }

	public string FormateRevisedDate => DateTimeHelper.ToDateTimeString(RevisedDate);

    /// <summary>
    /// 主分類編號
    /// </summary>
    public int? ProductTypeId { get; set; }

    /// <summary>
    /// 主分類簡碼
    /// </summary>
    public string ProductTypeCode { get; set; } = string.Empty;

    /// <summary>
    /// 商品狀態標籤
    /// </summary>
    [Display(Name = "商品標籤")]
    public override string Badge { get; set; } = string.Empty;

    /// <summary>
    /// 商品狀態標籤
    /// </summary>
    public override string BadgeName { get; set; } = string.Empty;

    /// <summary>
    /// 分類敘述
    /// </summary>
    public override List<string> ProductTypeDesc { get; set; }

    /// <summary>
    /// 分類敘述
    /// </summary>
    public List<ProdProductTypeDto> Types { get; set; } = new();

	public ProdSeoConfigDto? Seo { get; set; }

	public List<ProdProductSkuDto> Skus { get; set; } = new List<ProdProductSkuDto>();
	public List<ProdSpecificationConfigDto> SpecConfigs { get; set; } = new();

	public List<ProductImageDto>? Images { get; set; } = new();

    public List<ProductIngredientDto>? Ingredients { get; set; } = new();

    public List<ProductAttributeDto>? Attributes { get; set; } = new();

    public List<ProductReviewDto>? Reviews { get; set; }

    public List<ProductQuestionDto>? Questions { get; set; }

    /// <summary>
    /// 計算總數
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// UPC 代碼
    /// </summary>
    public string UpcCode => $"BC{ProductId.ToString().PadLeft(6, '0')}";

    /// <summary>
    /// 產品尺寸
    /// </summary>
    public string Dimensions => $"11.3 x 6.2 x 6.2 cm";

    /// <summary>
    /// 包裝規格
    /// </summary>
    public string? PackageType { get; set; }

    /// <summary>
    /// 評價星數
    /// </summary>
    public override decimal? AvgRating { get; set; }

    /// <summary>
    /// 評價總數
    /// </summary>
    public override int? ReviewCount { get; set; }

    /// <summary>
    /// 商品主圖
    /// </summary>
    public override string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// 主商品SkuId
    /// </summary>
    public int? MainSkuId { get; set; }

    /// <summary>
    /// 主商品原價
    /// </summary>
    public override decimal? ListPrice { get; set; }


    /// <summary>
    /// 主商品單價
    /// </summary>
    public override decimal? UnitPrice { get; set; }

    /// <summary>
    /// 主商品優惠價
    /// </summary>
    public override decimal? SalePrice { get; set; }

	/// <summary>
	/// 結帳單價
	/// </summary>
	private static decimal? FirstPositive(params decimal?[] prices)
=> prices.FirstOrDefault(p => p.HasValue && p.Value > 0);

	public decimal? BillingPrice => FirstPositive(SalePrice, UnitPrice, ListPrice);
}