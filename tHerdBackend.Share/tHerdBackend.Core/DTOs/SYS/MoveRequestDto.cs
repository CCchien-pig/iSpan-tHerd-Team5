namespace tHerdBackend.Core.DTOs.SYS
{
    public class MoveRequestDto
    {
        public List<int> Ids { get; set; } = new();
        public int? FolderId { get; set; }
    }
}
