using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechShop_API.Models.Dto
{
    public class ShoppingCartDTO
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public List<CartItemDTO> CartItems { get; set; } = new();
        public decimal CartTotal { get; set; }
    }

    public class CartItemDTO
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        public ProductDTO Product { get; set; }
    }

    public class ProductDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public List<string> ProductImages { get; set; }
    }
}
