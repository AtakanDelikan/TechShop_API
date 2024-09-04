using System.ComponentModel.DataAnnotations;

namespace TechShop_API.Models.Dto
{
    public class LaptopUpdateDTO
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        [Range(1, int.MaxValue)]
        public double Price { get; set; }
        public string CPU { get; set; }
        public string GPU { get; set; }
        public int Storage { get; set; }
        public double ScreenSize { get; set; }
        public string Resolution { get; set; }
        public string Brand { get; set; }
        public int Stock { get; set; }
        [Required]
        public string Image { get; set; }
    }
}
