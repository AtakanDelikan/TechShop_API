using System.ComponentModel.DataAnnotations;

namespace TechShop_API.Models.Dto
{
    public class OrderDetailCreateDTO
    {
        [Required]
        public int ProductId { get; set; }
        [Required]
        public int Quantity { get; set; }
        [Required]
        public string ItemName { get; set; }
        [Required]
        public decimal Price { get; set; }
    }
}
