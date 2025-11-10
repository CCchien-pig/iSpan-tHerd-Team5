namespace tHerdBackend.Core.DTOs.PROD
{
    public class AttributeFilterDto
    {
        public int AttributeId { get; set; }
        public List<string> ValueNames { get; set; } = new();
    }
}
