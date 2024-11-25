using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TechShop_API.Utility;

namespace TechShop_API.Models.Dto
{
    public class CategoryAttributeCreateDTO
    {
        public int CategoryId { get; set; }
        public string AttributeName { get; set; }
        public SD.DataTypeEnum DataType { get; set; }
    }
}
