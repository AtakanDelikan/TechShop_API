using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechShop_API.Models
{
    public class CategoryAttribute
    {
        [Key]
        public int Id { get; set; }
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category Category { get; set; }
        public string AttributeName { get; set; } = string.Empty;
        public string DataType { get; set; } = "string"; //TODO create types in SD.cs
    }
}
