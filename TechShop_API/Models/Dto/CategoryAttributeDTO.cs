
using TechShop_API.Utility;

namespace TechShop_API.Models.Dto
{
    public class CategoryAttributeDTO
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string AttributeName { get; set; } = string.Empty;
        public SD.DataTypeEnum DataType { get; set; } = SD.DataTypeEnum.String;
        public List<string> UniqueValues { get; set; } = new List<string>();
        public double? Min { get; set; }
        public double? Max { get; set; }
    }
}
