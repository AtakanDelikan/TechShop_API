using System.ComponentModel.DataAnnotations.Schema;

namespace TechShop_API.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public int LaptopId { get; set; }
        [ForeignKey("LaptopId")]
        public Laptop Laptop { get; set; }
        public int Quantity { get; set; }
        public int ShoppingCartId { get; set; }
    }
}
