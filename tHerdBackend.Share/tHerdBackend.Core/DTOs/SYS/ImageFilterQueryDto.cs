namespace tHerdBackend.Core.DTOs.SYS
{
    /// <summary>
    /// 圖片查詢條件
    /// </summary>
    public class ImageFilterQueryDto
    {
        public string? FolderPath { get; set; } = "";
        public string? Module { get; set; } = "";  // 模組代碼 (例如 SYS, PROD)
        public string? Keyword { get; set; } = ""; // 關鍵字搜尋
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
