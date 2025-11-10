namespace tHerdBackend.Core.DTOs.PROD
{
    /// <summary>
    /// 商品屬性明細 DTO
    /// </summary>
    public class ProductAttributeDto
    {
        /// <summary>
        /// 屬性ID
        /// </summary>
        public int AttributeId { get; set; }

        /// <summary>
        /// 屬性名稱（例如：功效、性別、年齡層）
        /// </summary>
        public string AttributeName { get; set; } = string.Empty;

        /// <summary>
        /// 資料型別（text / number / check / select）
        /// </summary>
        public string DataType { get; set; } = "text";

        /// <summary>
        /// 選項ID（若為選擇型）
        /// </summary>
        public int? AttributeOptionId { get; set; }

        /// <summary>
        /// 選項名稱（例如：保濕、男性、18-25歲）
        /// </summary>
        public string? OptionName { get; set; }

        /// <summary>
        /// 自訂屬性值（當 DataType = text 或 number 時使用）
        /// </summary>
        public string? AttributeValue { get; set; }
    }
}
