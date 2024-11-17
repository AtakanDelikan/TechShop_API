using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TechShop_API.Models;

namespace TechShop_API.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions options)
            : base(options)
        { 

        }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Laptop> Laptops { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<OrderHeader> OrderHeaders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Laptop>().HasData(new Laptop
            {
                Id = 1,
                Name = "Test Laptop1",
                Description = "Great Laptop",
                Price = 699.99,
                CPU = "intell i3",
                GPU = "GTX 1050ti",
                Storage = 256,
                ScreenSize = 14.1,
                Resolution = "1280 x 720",
                Brand = "Acer",
                Stock = 22,
                Image = "https://images.unsplash.com/photo-1522199755839-a2bacb67c546?q=80&w=500&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D"
            }, new Laptop
            {
                Id = 2,
                Name = "Test Laptop2",
                Description = "Greater Laptop",
                Price = 999.99,
                CPU = "intell i5",
                GPU = "GTX 1070ti",
                Storage = 512,
                ScreenSize = 15.3,
                Resolution = "1920 x 1080",
                Brand = "Apple",
                Stock = 24,
                Image = "https://images.unsplash.com/photo-1611186871348-b1ce696e52c9?q=80&w=500&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D"
            }, new Laptop
            {
                Id = 3,
                Name = "Test Laptop3",
                Description = "Greatest Laptop",
                Price = 1999.99,
                CPU = "intell i9",
                GPU = "GTX 2080ti",
                Storage = 1024,
                ScreenSize = 17.3,
                Resolution = "2560 × 1440",
                Brand = "Monster",
                Stock = 18,
                Image = "https://images.unsplash.com/photo-1640955014216-75201056c829?q=80&w=500&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D"
            });
        }
    }
}
