using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechShop_API.Models.Dto
{
    public class CategoryAttributeCsvDTO
    {
        public string CategoryName { get; set; }
        public string AttributeName { get; set; }
        public string DataType { get; set; }
    }
}
