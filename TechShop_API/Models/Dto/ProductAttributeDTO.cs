namespace TechShop_API.Models.Dto
{
    public class ProductAttributeDTO
    {
        public int Id { get; set; }
        public int CategoryAttributeId { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
