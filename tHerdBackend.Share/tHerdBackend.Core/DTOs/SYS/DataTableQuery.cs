namespace tHerdBackend.Core.DTOs.SYS
{
    public class DataTableQuery
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public string? SearchValue { get; set; }
        public string? Module { get; set; }
    }
}
