using System.ComponentModel.DataAnnotations;

namespace TechShop_API.Models.Dto
{
    public class UserDetailsDTO
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
    }
}
