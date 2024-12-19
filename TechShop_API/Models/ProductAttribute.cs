using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechShop_API.Models
{
    public class ProductAttribute
    {
        [Key]
        public int Id { get; set; }
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; }
        public int CategoryAttributeId { get; set; }
        [ForeignKey("CategoryAttributeId")]
        public CategoryAttribute CategoryAttribute { get; set; }
        public string? String { get; set; }
        public int? Integer { get; set; }
        public double? Decimal { get; set; }
        public bool? Boolean { get; set; }
    }
}
