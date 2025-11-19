using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Services;
using Xunit;

namespace TechShop_API.Tests.Services
{
    public class ProductFilterServiceTests
    {
        private ApplicationDbContext GetInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_" + System.Guid.NewGuid())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public void ApplyFilters_CategoryFilter_Works()
        {
            using var db = GetInMemoryDb();
            db.Categories.Add(new Category { Id = 1, Name = "Cat1" });
            db.Products.AddRange(
                new Product { Id = 1, CategoryId = 1, Name = "A" },
                new Product { Id = 2, CategoryId = 2, Name = "B" });
            db.SaveChanges();

            var service = new ProductFilterService(db);

            var filters = new Dictionary<string, string> { { "category", "1" } };
            var result = service.ApplyFilters(db.Products.AsQueryable(), filters).ToList();

            Assert.Single(result);
            Assert.Equal(1, result[0].CategoryId);
        }

        [Fact]
        public void ApplyFilters_PriceFilter_Works()
        {
            using var db = GetInMemoryDb();
            db.Products.AddRange(
                new Product { Id = 1, Price = 10 },
                new Product { Id = 2, Price = 20 },
                new Product { Id = 3, Price = 30 });
            db.SaveChanges();

            var service = new ProductFilterService(db);
            var filters = new Dictionary<string, string> { { "price", "15﹐25" } };

            var result = service.ApplyFilters(db.Products.AsQueryable(), filters).ToList();

            Assert.Single(result);
            Assert.Equal(20, result[0].Price);
        }

        [Fact]
        public void ApplyFilters_SearchFilter_Works()
        {
            using var db = GetInMemoryDb();
            db.Products.AddRange(
                new Product { Id = 1, Name = "Apple", Description = "Fruit" },
                new Product { Id = 2, Name = "Banana", Description = "Fruit" });
            db.SaveChanges();

            var service = new ProductFilterService(db);
            var filters = new Dictionary<string, string> { { "search", "Banana" } };

            var result = service.ApplyFilters(db.Products.AsQueryable(), filters).ToList();

            Assert.Single(result);
            Assert.Equal("Banana", result[0].Name);
        }
    }
}
