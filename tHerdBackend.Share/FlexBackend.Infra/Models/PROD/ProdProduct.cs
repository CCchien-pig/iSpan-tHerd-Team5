namespace FlexBackend.Infra.Models.PROD;

/// <summary>
/// 商品基本資料
/// </summary>
public partial class ProdProduct
{
    /// <summary>
    /// 商品ID
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// 供應商ID(FK)
    /// </summary>
    public int SupplierId { get; set; }

    /// <summary>
    /// 品牌ID
    /// </summary>
    public int? BrandId { get; set; }

    /// <summary>
    /// Seo設定
    /// </summary>
    public int? SeoId { get; set; }

    /// <summary>
    /// 商品名稱
    /// </summary>
    public string ProductName { get; set; }

    /// <summary>
    /// 商品簡短描述，常用於列表展示
    /// </summary>
    public string ShortDesc { get; set; }

    /// <summary>
    /// 商品完整描述，用於詳細頁
    /// </summary>
    public string FullDesc { get; set; }

    /// <summary>
    /// 是否上架（0=否，1=是）
    /// </summary>
    public bool IsPublished { get; set; }

    /// <summary>
    /// 重量（公斤）
    /// </summary>
    public decimal? Weight { get; set; }

    /// <summary>
    /// 體積
    /// </summary>
    public decimal? VolumeCubicMeter { get; set; }

    /// <summary>
    /// 體積單位
    /// </summary>
    public string VolumeUnit { get; set; }

    /// <summary>
    /// 建檔人員
    /// </summary>
    public int Creator { get; set; }

    /// <summary>
    /// 建立時間
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// 異動人員
    /// </summary>
    public int? Reviser { get; set; }

    /// <summary>
    /// 異動時間
    /// </summary>
    public DateTime? RevisedDate { get; set; }
}