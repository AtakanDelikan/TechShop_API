namespace TechShop_API.Models.Dto
{
    public class ProductDetailsDTO
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public List<string> Images { get; set; } = new List<string>();
        public List<ProductAttributeDTO> ProductAttributes { get; set; } = new List<ProductAttributeDTO>();
    }
}
