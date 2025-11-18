namespace TechShop_API.Models.Dto
{
    public class ProductAttributeCreateDTO
    {
        public int ProductId { get; set; }
        public int CategoryAttributeId { get; set; }
        public string Value { get; set; } = string.Empty;
    }

    public class ProductAttributeUpdateDTO
    {
        public int ProductId { get; set; }
        public int CategoryAttributeId { get; set; }
        public string Value { get; set; } = string.Empty;
    }

    public class ProductAttributeResponseDTO
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int CategoryAttributeId { get; set; }

        // Enriched metadata
        public string AttributeName { get; set; } = string.Empty;
        public TechShop_API.Utility.SD.DataTypeEnum DataType { get; set; }

        // Stored value as string for display
        public string? Value { get; set; }
    }
}
