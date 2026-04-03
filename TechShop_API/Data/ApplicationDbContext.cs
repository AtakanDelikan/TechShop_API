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

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var modifiedProducts = ChangeTracker.Entries<Product>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .Select(e => e.Entity);

            foreach (var product in modifiedProducts)
            {
                // Combine all searchable text into one giant string.
                if (product.Category == null && product.CategoryId > 0)
                {
                    await this.Entry(product).Reference(p => p.Category).LoadAsync(cancellationToken);
                }

                if (product.ProductAttributes == null)
                {
                    await this.Entry(product).Collection(p => p.ProductAttributes).LoadAsync(cancellationToken);
                }
                var attrStrings = product.ProductAttributes != null
                    ? string.Join(" ", product.ProductAttributes.Where(a => a.String != null).Select(a => a.String))
                    : "";

                var categoryName = product.Category?.Name ?? "";

                // Create a space-separated search vector  |product name|product description|category name|string attributes|
                product.SearchText = $"{product.Name} {product.Description} {categoryName} {attrStrings}".ToLower().Trim();
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
