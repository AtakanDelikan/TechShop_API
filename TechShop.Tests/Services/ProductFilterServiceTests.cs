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
            var filters = new Dictionary<string, string> { { "price", "15~25" } };

            var result = service.ApplyFilters(db.Products.AsQueryable(), filters).ToList();

            Assert.Single(result);
            Assert.Equal(20, result[0].Price);
        }

        [Fact]
        public void ApplyFilters_SearchFilter_Works()
        {
            using var db = GetInMemoryDb();
            db.Products.AddRange(
                new Product { Id = 1, Name = "Apple", Description = "Fruit", SearchText = "apple fruit" },
                new Product { Id = 2, Name = "Banana", Description = "Fruit", SearchText = "banana fruit" });
            db.SaveChanges();

            var service = new ProductFilterService(db);
            var filters = new Dictionary<string, string> { { "search", "Banana" } };

            var result = service.ApplyFilters(db.Products.AsQueryable(), filters).ToList();

            Assert.Single(result);
            Assert.Equal("Banana", result[0].Name);
        }

        // --- NULL & EMPTY CHECKS ---

        [Fact]
        public void ApplyFilters_ReturnsSource_WhenFiltersNullOrEmpty()
        {
            using var db = GetInMemoryDb();
            db.Products.Add(new Product { Id = 1, Name = "A" });
            db.SaveChanges();

            var service = new ProductFilterService(db);

            var nullResult = service.ApplyFilters(db.Products.AsQueryable(), null).ToList();
            var emptyResult = service.ApplyFilters(db.Products.AsQueryable(), new Dictionary<string, string>()).ToList();

            Assert.Single(nullResult);
            Assert.Single(emptyResult);
        }

        // --- PRICE EDGE CASES ---

        [Theory]
        [InlineData("10")] // No tilde
        [InlineData("10~")] // Missing max
        [InlineData("~20")] // Missing min
        [InlineData("abc~def")] // Not numbers
        [InlineData("30~20")] // Min greater than max
        public void ApplyFilters_IgnoresPriceFilter_WhenFormatIsInvalid(string invalidPriceStr)
        {
            using var db = GetInMemoryDb();
            db.Products.Add(new Product { Id = 1, Price = 15 });
            db.Products.Add(new Product { Id = 2, Price = 25 });
            db.SaveChanges();

            var service = new ProductFilterService(db);
            var filters = new Dictionary<string, string> { { "price", invalidPriceStr } };

            var result = service.ApplyFilters(db.Products.AsQueryable(), filters).ToList();

            // Filter should be safely ignored, returning all products
            Assert.Equal(2, result.Count);
        }

        // --- SORTING ---

        [Theory]
        [InlineData("price_asc", 1, 2, 3)]
        [InlineData("price_desc", 3, 2, 1)]
        [InlineData("newest", 3, 2, 1)]
        [InlineData("rating", 2, 3, 1)] // P2 has highest rating (5.0)
        [InlineData("unknown_sort", 1, 2, 3)] // Falls back to OrderBy Name (A, B, C)
        public void ApplyFilters_SortsCorrectly(string sortOrder, int expectedFirst, int expectedSecond, int expectedThird)
        {
            using var db = GetInMemoryDb();
            db.Products.AddRange(
                new Product { Id = 1, Name = "A", Price = 10, Rating = 4.0 },
                new Product { Id = 2, Name = "B", Price = 20, Rating = 5.0 }, // Highest rating
                new Product { Id = 3, Name = "C", Price = 30, Rating = 4.5 }  // Newest (Highest ID)
            );
            db.SaveChanges();

            var service = new ProductFilterService(db);
            var filters = new Dictionary<string, string> { { "sort", sortOrder } };

            var result = service.ApplyFilters(db.Products.AsQueryable(), filters).ToList();

            Assert.Equal(3, result.Count);
            Assert.Equal(expectedFirst, result[0].Id);
            Assert.Equal(expectedSecond, result[1].Id);
            Assert.Equal(expectedThird, result[2].Id);
        }

        // --- ATTRIBUTE FILTERS (HAPPY & UNHAPPY) ---

        [Fact]
        public void ApplyFilters_IgnoresMalformedAttributeStrings()
        {
            using var db = GetInMemoryDb();
            db.Products.Add(new Product { Id = 1, Name = "A" });
            db.SaveChanges();

            var service = new ProductFilterService(db);

            // Missing brackets, missing semi-colons, non-numeric IDs
            var filters = new Dictionary<string, string> { { "attributes", "abc[val];1val];2[val" } };

            var result = service.ApplyFilters(db.Products.AsQueryable(), filters).ToList();

            // Should safely ignore bad formats and return the product
            Assert.Single(result);
        }

        [Fact]
        public void ApplyFilters_AttributeString_MatchesCorrectly()
        {
            using var db = GetInMemoryDb();
            db.CategoryAttributes.Add(new CategoryAttribute { Id = 1, DataType = TechShop_API.Utility.SD.DataTypeEnum.String });
            db.Products.AddRange(new Product { Id = 1 }, new Product { Id = 2 });
            db.ProductAttributes.AddRange(
                new ProductAttribute { ProductId = 1, CategoryAttributeId = 1, String = "Red" },
                new ProductAttribute { ProductId = 2, CategoryAttributeId = 1, String = "Blue" }
            );
            db.SaveChanges();

            var service = new ProductFilterService(db);
            var filters = new Dictionary<string, string> { { "attributes", "1[Red~Green]" } }; // Looking for Red OR Green

            var result = service.ApplyFilters(db.Products.AsQueryable(), filters).ToList();

            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }

        [Fact]
        public void ApplyFilters_AttributeInteger_MatchesRangeCorrectly()
        {
            using var db = GetInMemoryDb();
            db.CategoryAttributes.Add(new CategoryAttribute { Id = 2, DataType = TechShop_API.Utility.SD.DataTypeEnum.Integer });
            db.Products.AddRange(new Product { Id = 1 }, new Product { Id = 2 }, new Product { Id = 3 });
            db.ProductAttributes.AddRange(
                new ProductAttribute { ProductId = 1, CategoryAttributeId = 2, Integer = 8 },
                new ProductAttribute { ProductId = 2, CategoryAttributeId = 2, Integer = 15 },
                new ProductAttribute { ProductId = 3, CategoryAttributeId = 2, Integer = 25 }
            );
            db.SaveChanges();

            var service = new ProductFilterService(db);
            // Needs exactly 2 values for min/max
            var filters = new Dictionary<string, string> { { "attributes", "2[10~20]" } };

            var result = service.ApplyFilters(db.Products.AsQueryable(), filters).ToList();

            Assert.Single(result);
            Assert.Equal(2, result[0].Id);
        }

        [Fact]
        public void ApplyFilters_AttributeBoolean_MatchesCorrectly_AndIgnoresInvalid()
        {
            using var db = GetInMemoryDb();
            db.CategoryAttributes.Add(new CategoryAttribute { Id = 3, DataType = TechShop_API.Utility.SD.DataTypeEnum.Boolean });
            db.Products.AddRange(new Product { Id = 1 }, new Product { Id = 2 });
            db.ProductAttributes.AddRange(
                new ProductAttribute { ProductId = 1, CategoryAttributeId = 3, Boolean = true },
                new ProductAttribute { ProductId = 2, CategoryAttributeId = 3, Boolean = false }
            );
            db.SaveChanges();

            var service = new ProductFilterService(db);

            // Test 1: Valid True
            var filtersTrue = new Dictionary<string, string> { { "attributes", "3[true]" } };
            var resultTrue = service.ApplyFilters(db.Products.AsQueryable(), filtersTrue).ToList();
            Assert.Single(resultTrue);
            Assert.Equal(1, resultTrue[0].Id);

            // Test 2: Invalid Boolean format (should ignore filter and return all)
            var filtersInvalid = new Dictionary<string, string> { { "attributes", "3[maybe]" } };
            var resultInvalid = service.ApplyFilters(db.Products.AsQueryable(), filtersInvalid).ToList();
            Assert.Equal(2, resultInvalid.Count);
        }
    }
}
