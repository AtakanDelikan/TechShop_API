using Microsoft.AspNetCore.Identity;

namespace TechShop_API.Models
{
    public class ApplicationUser : IdentityUser
    {
        public required string Name { get; set; }
        public string? Address { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }
    }
}
