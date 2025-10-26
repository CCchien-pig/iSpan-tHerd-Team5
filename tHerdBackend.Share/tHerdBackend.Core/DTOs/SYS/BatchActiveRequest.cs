namespace tHerdBackend.Core.DTOs.SYS
{
    public class BatchActiveRequest
    {
        public List<int> Ids { get; set; } = new();
        public bool IsActive { get; set; }
    }
}
