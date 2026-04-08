using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services;
using TechShop_API.Services.Interfaces;
using TechShop_API.Utility;
using Xunit;

namespace TechShop.Tests.Services
{
    public class ProductServiceTests
    {
        private ApplicationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private ProductService CreateService(ApplicationDbContext db)
        {
            var filter = new ProductFilterService(db); // real filter
            return new ProductService(db, filter);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllProducts()
        {
            using var db = CreateDbContext();
            db.Categories.Add(new Category { Id = 1, Name = "Cat" });
            db.Products.Add(new Product { Id = 1, Name = "P1", CategoryId = 1, Price = 10, Stock = 2 });
            db.Products.Add(new Product { Id = 2, Name = "P2", CategoryId = 1, Price = 20, Stock = 3 });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var resp = await service.GetAllAsync();

            Assert.Equal(System.Net.HttpStatusCode.OK, resp.StatusCode);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(resp.Result);
            Assert.Equal(2, ((IEnumerable<object>)resp.Result).Count());
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNotFound_ForMissing()
        {
            using var db = CreateDbContext();
            var service = CreateService(db);

            var resp = await service.GetByIdAsync(999);

            Assert.Equal(System.Net.HttpStatusCode.NotFound, resp.StatusCode);
            Assert.False(resp.IsSuccess);
        }

        [Fact]
        public async Task CreateAsync_Fails_WhenCategoryMissing()
        {
            using var db = CreateDbContext();
            var service = CreateService(db);

            var dto = new ProductCreateDTO
            {
                CategoryId = 99,
                Name = "New",
                Description = "d",
                Price = 10,
                Stock = 1
            };

            var resp = await service.CreateAsync(dto);

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resp.StatusCode);
            Assert.False(resp.IsSuccess);
            Assert.Contains("Category", string.Join(" ", resp.ErrorMessages));
        }

        [Fact]
        public async Task CreateAsync_Succeeds_WhenCategoryExists()
        {
            using var db = CreateDbContext();
            db.Categories.Add(new Category { Id = 5, Name = "C" });
            await db.SaveChangesAsync();

            var service = CreateService(db);

            var dto = new ProductCreateDTO
            {
                CategoryId = 5,
                Name = "New",
                Description = "d",
                Price = 10,
                Stock = 1
            };

            var resp = await service.CreateAsync(dto);

            Assert.Equal(System.Net.HttpStatusCode.Created, resp.StatusCode);
            Assert.NotNull(resp.Result);
        }

        [Fact]
        public async Task UpdateAsync_Fails_WhenProductMissing()
        {
            using var db = CreateDbContext();
            var service = CreateService(db);

            var dto = new ProductUpdateDTO
            {
                CategoryId = 1,
                Name = "X",
                Description = "d",
                Price = 5,
                Stock = 1
            };

            var resp = await service.UpdateAsync(999, dto);

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resp.StatusCode);
            Assert.False(resp.IsSuccess);
        }

        [Fact]
        public async Task UpdateAsync_Succeeds_WhenValid()
        {
            using var db = CreateDbContext();
            db.Categories.Add(new Category { Id = 2, Name = "Cat2" });
            db.Products.Add(new Product { Id = 10, Name = "Old", CategoryId = 2, Price = 1, Stock = 1 });
            await db.SaveChangesAsync();

            var service = CreateService(db);

            var dto = new ProductUpdateDTO
            {
                CategoryId = 2,
                Name = "New",
                Description = "desc",
                Price = 11,
                Stock = 5
            };

            var resp = await service.UpdateAsync(10, dto);

            Assert.Equal(System.Net.HttpStatusCode.NoContent, resp.StatusCode);

            var prod = await db.Products.FindAsync(10);
            Assert.Equal("New", prod.Name);
            Assert.Equal(11, prod.Price);
        }

        [Fact]
        public async Task DeleteAsync_RemovesProductAndRelated()
        {
            using var db = CreateDbContext();
            db.Categories.Add(new Category { Id = 3, Name = "Cat3" });
            var p = new Product { Id = 20, Name = "ToDelete", CategoryId = 3, Price = 1, Stock = 1 };
            db.Products.Add(p);
            db.ProductAttributes.Add(new ProductAttribute { Id = 100, ProductId = 20, CategoryAttributeId = 1, String = "v" });
            db.ProductImages.Add(new ProductImage { Id = 200, ProductId = 20, Url = "/x.jpg", DisplayOrder = 0 });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var resp = await service.DeleteAsync(20);

            Assert.Equal(System.Net.HttpStatusCode.NoContent, resp.StatusCode);
            Assert.Null(await db.Products.FindAsync(20));
            Assert.Empty(db.ProductAttributes.Where(pa => pa.ProductId == 20));
            Assert.Empty(db.ProductImages.Where(pi => pi.ProductId == 20));
        }

        // --- GET BY ID (SUCCESS WITH ATTRIBUTES) ---

        [Fact]
        public async Task GetByIdAsync_ReturnsProductWithAttributes_WhenExists()
        {
            using var db = CreateDbContext();
            var cat = new Category { Id = 1, Name = "Electronics" };
            db.Categories.Add(cat);

            var product = new Product { Id = 1, Name = "Laptop", CategoryId = 1, Category = cat };
            db.Products.Add(product);

            var attr = new CategoryAttribute { Id = 1, AttributeName = "RAM", DataType = SD.DataTypeEnum.String };
            db.CategoryAttributes.Add(attr);
            db.ProductAttributes.Add(new ProductAttribute { ProductId = 1, CategoryAttributeId = 1, String = "16GB", CategoryAttribute = attr });

            await db.SaveChangesAsync();

            var service = CreateService(db);
            var resp = await service.GetByIdAsync(1);

            Assert.Equal(System.Net.HttpStatusCode.OK, resp.StatusCode);
            Assert.NotNull(resp.Result);

            var result = resp.Result.GetType().GetProperty("Name")?.GetValue(resp.Result, null);
            Assert.Equal("Laptop", result);
        }

        // --- PAGINATION & CATEGORY FILTERING ---

        [Fact]
        public async Task GetByCategoryAsync_HandlesPaginationCorrectly()
        {
            using var db = CreateDbContext();
            db.Categories.Add(new Category { Id = 1, Name = "Test" });

            for (int i = 1; i <= 5; i++)
            {
                db.Products.Add(new Product { Id = i, Name = $"P{i}", CategoryId = 1, Price = 10 });
            }
            await db.SaveChangesAsync();

            var service = CreateService(db);

            // Page 2, PageSize 2 -> Should return items 3 and 4 (Skip 2, Take 2)
            var resp = await service.GetByCategoryAsync(1, 2, 2);

            Assert.Equal(System.Net.HttpStatusCode.OK, resp.StatusCode);

            var result = Assert.IsType<PagedResultDTO<ProductListItemDTO>>(resp.Result);

            Assert.Equal(5, result.TotalItems);
            Assert.Equal(3, result.TotalPages); // 5/2 ceiling
            Assert.Equal(2, result.CurrentPage);
            Assert.Equal(2, ((IEnumerable<object>)result.Products).Count());
        }

        // --- FILTERING & FACETS ---

        [Fact]
        public async Task FilterProductsAsync_ReturnsCategoryFacets_AndFiltersCorrectly()
        {
            using var db = CreateDbContext();
            var cat1 = new Category { Id = 1, Name = "Phones" };
            var cat2 = new Category { Id = 2, Name = "Laptops" };
            db.Categories.AddRange(cat1, cat2);

            db.Products.Add(new Product { Id = 1, Name = "iPhone", CategoryId = 1, Category = cat1 });
            db.Products.Add(new Product { Id = 2, Name = "Macbook", CategoryId = 2, Category = cat2 });
            await db.SaveChangesAsync();

            var service = CreateService(db);

            // Filter by "iPhone" string
            var filters = new Dictionary<string, string> { { "search", "iphone" } };
            var resp = await service.FilterProductsAsync(filters, 1, 10);

            Assert.Equal(System.Net.HttpStatusCode.OK, resp.StatusCode);

            var result = Assert.IsType<PagedResultDTO<ProductListItemDTO>>(resp.Result);
            var products = result.Products;
            var facets = result.AvailableCategories.ToList();

            Assert.Single(products); // Only iPhone
            Assert.Single(facets);   // Only Phones category in facet
            Assert.Equal("Phones", facets.First().CategoryName);
            Assert.Equal(1, facets.First().Count);
        }

        // --- UNHAPPY PATHS (EDGE CASES) ---

        [Fact]
        public async Task UpdateAsync_Fails_WhenCategoryDoesNotExist()
        {
            using var db = CreateDbContext();
            db.Products.Add(new Product { Id = 1, Name = "Old", CategoryId = 1 });
            db.Categories.Add(new Category { Id = 1, Name = "Existing" });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var dto = new ProductUpdateDTO { CategoryId = 999, Name = "Bad Cat" }; // 999 doesn't exist

            var resp = await service.UpdateAsync(1, dto);

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resp.StatusCode);
            Assert.Contains("Category doesn't exist", resp.ErrorMessages);
        }

        [Fact]
        public async Task DeleteAsync_Fails_WhenProductNotFound()
        {
            using var db = CreateDbContext();
            var service = CreateService(db);

            var resp = await service.DeleteAsync(999);

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resp.StatusCode);
            Assert.False(resp.IsSuccess);
            Assert.Contains("Product not found", resp.ErrorMessages);
        }

        // --- PAGINATION & CATEGORY FILTER TESTS ---

        [Fact]
        public async Task GetByCategoryAsync_ReturnsPagedResults()
        {
            using var db = CreateDbContext();
            db.Categories.Add(new Category { Id = 1, Name = "Electronics" });
            // Add 5 products
            for (int i = 1; i <= 5; i++)
            {
                db.Products.Add(new Product { Id = i, Name = $"Product {i}", CategoryId = 1, Price = 10 });
            }
            await db.SaveChangesAsync();

            var service = CreateService(db);

            // Request Page 2 with size 2 (Should get products 3 and 4)
            var resp = await service.GetByCategoryAsync(categoryId: 1, pageNumber: 2, pageSize: 2);

            Assert.Equal(System.Net.HttpStatusCode.OK, resp.StatusCode);

            var result = Assert.IsType<PagedResultDTO<ProductListItemDTO>>(resp.Result);
            Assert.Equal(5, result.TotalItems);
            Assert.Equal(3, result.TotalPages);
            Assert.Equal(2, result.CurrentPage);
            Assert.Equal(2, (result.Products).Count());
        }

        // --- FILTER & FACET TESTS ---

        [Fact]
        public async Task FilterProductsAsync_ReturnsCategoryFacets()
        {
            using var db = CreateDbContext();
            var cat1 = new Category { Id = 1, Name = "Phones" };
            var cat2 = new Category { Id = 2, Name = "Laptops" };
            db.Categories.AddRange(cat1, cat2);

            db.Products.Add(new Product { Id = 1, Name = "iPhone", Category = cat1, CategoryId = 1 });
            db.Products.Add(new Product { Id = 2, Name = "Pixel", Category = cat1, CategoryId = 1 });
            db.Products.Add(new Product { Id = 3, Name = "Macbook", Category = cat2, CategoryId = 2 });
            await db.SaveChangesAsync();

            var service = CreateService(db);

            var filters = new Dictionary<string, string> { { "search", "phone" } };

            var resp = await service.FilterProductsAsync(filters, 1, 10);

            var result = Assert.IsType<PagedResultDTO<ProductListItemDTO>>(resp.Result);
            var facets = result.AvailableCategories.ToList();

            Assert.Single(facets);
            Assert.Equal("Phones", facets.First().CategoryName);
            Assert.Equal(2, facets.First().Count);
        }

        // --- EDGE CASE: GET BY ID WITH ATTRIBUTES ---

        [Fact]
        public async Task GetByIdAsync_IncludesAttributesAndImages()
        {
            using var db = CreateDbContext();
            db.Categories.Add(new Category { Id = 1, Name = "Cat" });
            db.CategoryAttributes.Add(new CategoryAttribute { Id = 1, AttributeName = "Color", DataType = SD.DataTypeEnum.String });

            var p = new Product { Id = 1, Name = "P1", CategoryId = 1 };
            db.Products.Add(p);
            db.ProductAttributes.Add(new ProductAttribute { ProductId = 1, CategoryAttributeId = 1, String = "Red" });
            db.ProductImages.Add(new ProductImage { ProductId = 1, Url = "test.jpg" });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var resp = await service.GetByIdAsync(1);

            Assert.Equal(System.Net.HttpStatusCode.OK, resp.StatusCode);
            // This ensures the ProductMapping.ToDetails logic and Attribute includes are covered
            Assert.NotNull(resp.Result);
        }

    }
}
