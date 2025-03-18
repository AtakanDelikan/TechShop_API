using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
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
        public DbSet<Product> Products { get; set; }
        public DbSet<CategoryAttribute> CategoryAttributes { get; set; }
        public DbSet<ProductAttribute> ProductAttributes { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Comment> Comments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            foreach (var foreignKey in builder.Model.GetEntityTypes()
                .SelectMany(e => e.GetForeignKeys()))
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }

            builder.Entity<Product>()
            .Property(p => p.Price)
            .HasColumnType("DECIMAL(18, 2)");

            builder.Entity<OrderDetail>()
            .Property(od => od.Price)
            .HasColumnType("DECIMAL(18, 2)");

            builder.Entity<OrderHeader>()
            .Property(oh => oh.OrderTotal)
            .HasColumnType("DECIMAL(18, 2)");

            builder.Entity<Category>()
            .HasIndex(c => c.Name)
            .IsUnique();

            base.OnModelCreating(builder);
        }
    }
}
