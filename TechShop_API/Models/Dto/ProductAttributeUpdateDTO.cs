using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TechShop_API.Utility;

namespace TechShop_API.Models.Dto
{
    public class ProductAttributeUpdateDTO
    {
        public string Value { get; set; }
    }
}
