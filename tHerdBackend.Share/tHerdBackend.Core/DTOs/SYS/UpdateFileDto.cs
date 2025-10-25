namespace tHerdBackend.Core.DTOs.SYS
{
    public class UpdateFileDto
    {
        public int FileId { get; set; }
        public string? AltText { get; set; }
        public string? Caption { get; set; }
        public bool IsActive { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
    }
}
