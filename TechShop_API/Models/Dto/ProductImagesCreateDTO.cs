using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechShop_API.Models.Dto
{
    public class ProductImagesCreateDTO
    {
        public int ProductId { get; set; }
        public string[] Urls { get; set; }
    }
}
