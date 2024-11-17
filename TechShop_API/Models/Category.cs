using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TechShop_API.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Name of the category
        public string Description { get; set; } = string.Empty; // Description (optional)
        public int? ParentCategoryId { get; set; } // Nullable for top-level categories
        [ForeignKey("ParentCategoryId")]
        public Category? ParentCategory { get; set; } // Navigation property for parent
        //public ICollection<Category>? SubCategories { get; set; } // Subcategories
    }
}
