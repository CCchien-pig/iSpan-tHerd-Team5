namespace tHerdBackend.Core.DTOs.PROD
{
    public class AttributeWithOptionsDto
    {
        public int AttributeId { get; set; }
        public string AttributeName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public List<AttributeOptionDto> Options { get; set; } = new();
    }

    public class AttributeOptionDto
    {
        public int AttributeOptionId { get; set; }
        public string OptionName { get; set; } = string.Empty;
        public string? OptionValue { get; set; }
        public int? OrderSeq { get; set; }
    }
}
