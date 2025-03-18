using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechShop_API.Models.Dto
{
    public class CategoryCsvDTO
    {
        public string Name { get; set; }
        public string Parent { get; set; }
        public string Description { get; set; }
    }
}
