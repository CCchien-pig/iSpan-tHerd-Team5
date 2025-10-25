namespace tHerdBackend.Core.DTOs.SYS
{
    /// <summary>
    /// 檔案總管用的通用資料模型
    /// </summary>
    public class FolderItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string? ModuleId { get; set; }
        public string? Url { get; set; }
        public string? MimeType { get; set; }
        public bool IsFolder { get; set; }
        public string? AltText { get; set; }
        public string? Caption { get; set; }
        public bool? IsActive { get; set; }
    }
}
