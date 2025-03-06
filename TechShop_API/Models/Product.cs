using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TechShop_API.Models.Dto;

namespace TechShop_API.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category Category { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        [Range(1, int.MaxValue)]
        public double Rating { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public ICollection<ProductImage> ProductImages { get; set; }
        [NotMapped]
        public ICollection<ProductAttributeDTO> ProductAttributes { get; set; }
    }
}
