using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services;
using TechShop_API.Services.Interfaces;
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
    }
}
