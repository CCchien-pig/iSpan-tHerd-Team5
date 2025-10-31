namespace tHerdBackend.Core.DTOs.PROD;

public partial class ProductFilterQueryDto
{
	public int PageIndex { get; set; } = 1;     // 第幾頁
	public int PageSize { get; set; } = 20;     // 每頁筆數
 	public string? Keyword { get; set; }        // 關鍵字
	public int? ProductTypeId { get; set; }     // 類別編號
	public int? BrandId { get; set; }           // 品牌編號
    public decimal? MinPrice { get; set; }      // 最低價
	public decimal? MaxPrice { get; set; }      // 最高價
	public string? SortBy { get; set; }         // 排序欄位
    public bool SortDesc { get; set; } = false; // 是否倒序
    public bool? IsPublished { get; set; }      // 是否發佈
    public bool? IsFrontEnd { get; set; }       // 是否來自前端

    public int? ProductId { get; set; }
}

/// <summary>
/// 商品基本資料
/// </summary>
public partial class ProdProductQueryDto
{
    /// <summary>
    /// 商品ID
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// 商品名稱
    /// </summary>
    public string ProductName { get; set; }

    /// <summary>
    /// 供應商ID(FK)
    /// </summary>
    public int? SupId { get; set; }

    /// <summary>
    /// 供應商名稱
    /// </summary>
    public string SupName { get; set; }

    /// <summary>
    /// 品牌ID
    /// </summary>
    public int? BrandId { get; set; }

    /// <summary>
    /// 品牌名稱
    /// </summary>
    public string BrandName { get; set; }

    /// <summary>
    /// 折扣率 (0 ~ 100%)
    /// </summary>
    public decimal? BrandDisCntRate { get; set; } // DiscountRate

    /// <summary>
    /// 折扣狀態，1=有效、0=結束（排程更新）
    /// </summary>
    public bool BrandDisCntActive { get; set; } // IsDiscountActive

    /// <summary>
    /// 聯絡人
    /// </summary>
    public string SupContact { get; set; } // ContactName

    /// <summary>
    /// 聯絡電話
    /// </summary>
    public string SupPhone { get; set; } // Phone

    /// <summary>
    /// 電子郵件
    /// </summary>
    public string SupEmail { get; set; } // Email

    /// <summary>
    /// 商品簡短描述，常用於列表展示
    /// </summary>
    public string ShortDesc { get; set; }

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
    /// SKU 編號
    /// </summary>
    public int? SkuId { get; set; }

    /// <summary>
    /// 規格碼
    /// </summary
    public string SpecCode { get; set; }

    /// <summary>
    /// SKU代碼
    /// </summary>
    public string SkuCode { get; set; }

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
    public int? StockQty { get; set; }

    /// <summary>
    /// 安全庫存量（低於提醒）
    /// </summary>
    public int? SafetyStockQty { get; set; }

    /// <summary>
    /// 再訂購點（≧安全庫存量）
    /// </summary>
    public int? ReorderPoint { get; set; }

    /// <summary>
    /// 最大庫存量（0=不限制)
    /// </summary>
    public int? MaxStockQty { get; set; }

    /// <summary>
    /// 是否允許缺貨預購（1=可超賣，0=禁止）
    /// </summary>
    public bool? IsAllowBackorder { get; set; }

    /// <summary>
    /// 有效天數
    /// </summary>
    public int? ShelfLifeDays { get; set; }

    /// <summary>
    /// 商品分類
    /// </summary>
    public string ProductTypeName { get; set; }

    /// <summary>
    /// 分類清單
    /// </summary>
    public List<ProdProductTypeQueryDto> Types { get; set; } = new();

    /// <summary>
    /// 規格
    /// </summary>
    public string Spec { get; set; } // PROD_SpecificationConfig.GroupName

    /// <summary>
    /// 規格選項清單
    /// </summary>
    public List<ProdSpecificationQueryDto> SpecOptions { get; set; } = new();

    /// <summary>
    /// 規格
    /// </summary>
    public string Attribute { get; set; } // PROD_AttributeOption.Attribute

    /// <summary>
    /// 屬性選項清單
    /// </summary>
    public List<ProdAttributeOptionQueryDto> AttributeOptions { get; set; } = new();

    /// <summary>
    /// 組合
    /// </summary>
    public string Bundle { get; set; } // PROD_BundleItem.Bundle

    /// <summary>
    /// 組合清單
    /// </summary>
    public List<ProdBundleItemQueryDto> BundleItems { get; set; } = new();

    /// <summary>
    /// 組合
    /// </summary>
    public string Ingredient { get; set; } // PROD_ProductIngredient.Ingredients

    /// <summary>
    /// 成分清單
    /// </summary>
    public List<ProdProductIngredientQueryDto> Ingredients { get; set; } = new();
}

/// <summary>
/// SKU層級與即時庫存(支援多規格)
/// </summary>
public partial class ProdProductSkuQueryDto
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
}

/// <summary>
/// 套組明細表：記錄套組內有哪些子商品
/// </summary>
public partial class ProdBundleItemQueryDto
{
    /// <summary>
    /// 套組ID (FK)
    /// </summary>
    public int BundleId { get; set; }

    /// <summary>
    /// 商品ID (FK)
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// 套組名稱
    /// </summary>
    public string BundleName { get; set; }
}

/// <summary>
/// 屬性選項
/// </summary>
public partial class ProdAttributeOptionQueryDto
{
    /// <summary>
    /// 屬性選項ID（主鍵）
    /// </summary>
    public int AttributeOptionId { get; set; }

    /// <summary>
    /// 屬性ID（外鍵）
    /// </summary>
    public int AttributeId { get; set; }

    /// <summary>
    /// 屬性名稱（如：性別、年齡區間）
    /// </summary>
    public string AttributeName { get; set; }

    /// <summary>
    /// 選項名稱（如：男性、女性、18-25歲）
    /// </summary>
    public string OptionName { get; set; }

    /// <summary>
    /// 選項值（用於儲存額外代碼或數值）
    /// </summary>
    public string OptionValue { get; set; }
}

/// <summary>
/// 商品的分類明細表，一個商品可對應多個分類
/// </summary>
public partial class ProdProductTypeQueryDto
{
    /// <summary>
    /// 商品ID（外鍵）
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// 分類ID（外鍵）
    /// </summary>
    public int ProductTypeId { get; set; }
}

/// <summary>
/// 商品的規格設定的選項
/// </summary>
public partial class ProdSpecificationQueryDto
{
    /// <summary>
    /// 規格選項ID（主鍵）
    /// </summary>
    public int SpecificationOptionId { get; set; }

    /// <summary>
    /// SKU ID（外鍵）
    /// </summary>
    public int SkuId { get; set; }

    /// <summary>
    /// 規格群組ID（外鍵）
    /// </summary>
    public int SpecificationConfigId { get; set; }

    /// <summary>
    /// 規格群組名稱（例如：容量、口味、顏色）
    /// </summary>
    public string GroupName { get; set; }

    /// <summary>
    /// 規格選項名稱（例如：250ml、巧克力）
    /// </summary>
    public string OptionName { get; set; }
}

/// <summary>
/// 商品成分明細；一個商品可對應多個成分
/// </summary>
public partial class ProdProductIngredientQueryDto
{
    /// <summary>
    /// 商品ID（外鍵）
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// 成分ID（外鍵）
    /// </summary>
    public int IngredientId { get; set; }

    /// <summary>
    /// 百分比或含量（單位可於前台說明）
    /// </summary>
    public decimal? Percentage { get; set; }

    /// <summary>
    /// 劑量、產地或其他備註
    /// </summary>
    public string Note { get; set; }
}