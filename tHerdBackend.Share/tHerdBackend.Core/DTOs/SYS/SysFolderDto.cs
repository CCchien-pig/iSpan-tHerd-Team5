namespace tHerdBackend.Core.DTOs.SYS
{
    public class SysFolderDto
    {
        public int FolderId { get; set; }
        public string FolderName { get; set; } = "";
        public int? ParentId { get; set; }
    }
}
