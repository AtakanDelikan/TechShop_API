﻿using Microsoft.AspNetCore.Identity;

namespace TechShop_API.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
        public string Address { get; set; }
    }
}
