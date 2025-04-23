using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TechShop_API.Utility;

namespace TechShop_API.Models.Dto
{
    public class ProductAttributeUpdateDTO
    {
        public int ProductId { get; set; }
        public int CategoryAttributeId { get; set; }
        public string Value { get; set; }
    }
}
