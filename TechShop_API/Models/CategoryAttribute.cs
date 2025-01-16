using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using TechShop_API.Utility;

namespace TechShop_API.Models
{
    public class CategoryAttribute
    {
        [Key]
        public int Id { get; set; }
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        [JsonIgnore]
        public Category Category { get; set; }
        public string AttributeName { get; set; } = string.Empty;
        public SD.DataTypeEnum DataType { get; set; } = SD.DataTypeEnum.String;
        public List<string> UniqueValues { get; set; } = new List<string>();
        public double? Min { get; set; }
        public double? Max { get; set; }
    }
}
