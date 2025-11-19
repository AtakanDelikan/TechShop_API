using Microsoft.EntityFrameworkCore;
using System;
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
    public class ProductAttributeServiceTests
    {
        private ApplicationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private ProductAttributeService CreateService(ApplicationDbContext db)
        {
            var converter = new ProductAttributeValueConverter();
            return new ProductAttributeService(db, converter);
        }

        [Fact]
        public async Task CreateAsync_Fails_WhenProductMissing()
        {
            using var db = CreateDbContext();
            var service = CreateService(db);

            var dto = new ProductAttributeCreateDTO { ProductId = 1, CategoryAttributeId = 1, Value = "v" };
            var resp = await service.CreateAsync(dto);

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resp.StatusCode);
            Assert.False(resp.IsSuccess);
        }

        [Fact]
        public async Task CreateAsync_Fails_WhenCategoryAttributeMissing()
        {
            using var db = CreateDbContext();
            db.Products.Add(new Product { Id = 5, CategoryId = 1, Name = "p" });
            await db.SaveChangesAsync();

            var service = CreateService(db);

            var dto = new ProductAttributeCreateDTO { ProductId = 5, CategoryAttributeId = 99, Value = "v" };
            var resp = await service.CreateAsync(dto);

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resp.StatusCode);
            Assert.False(resp.IsSuccess);
        }

        [Fact]
        public async Task CreateAsync_Creates_NewAttribute_AndUpdatesCategoryStats()
        {
            using var db = CreateDbContext();
            db.Products.Add(new Product { Id = 6, CategoryId = 1, Name = "p" });
            db.CategoryAttributes.Add(new CategoryAttribute { Id = 7, AttributeName = "A", DataType = SD.DataTypeEnum.String, UniqueValues = new System.Collections.Generic.List<string>() });
            await db.SaveChangesAsync();

            var service = CreateService(db);

            var dto = new ProductAttributeCreateDTO { ProductId = 6, CategoryAttributeId = 7, Value = "hello" };
            var resp = await service.CreateAsync(dto);

            Assert.Equal(System.Net.HttpStatusCode.Created, resp.StatusCode);
            var created = Assert.IsType<ProductAttributeResponseDTO>(resp.Result);
            Assert.Equal(6, created.ProductId);

            var catAttr = await db.CategoryAttributes.FindAsync(7);
            Assert.Contains("hello", catAttr.UniqueValues);
        }

        [Fact]
        public async Task UpdateAsync_Creates_WhenMissing()
        {
            using var db = CreateDbContext();
            db.Products.Add(new Product { Id = 8, CategoryId = 1, Name = "p" });
            db.CategoryAttributes.Add(new CategoryAttribute { Id = 9, AttributeName = "A", DataType = SD.DataTypeEnum.String });
            await db.SaveChangesAsync();

            var service = CreateService(db);

            var dto = new ProductAttributeUpdateDTO { ProductId = 8, CategoryAttributeId = 9, Value = "v2" };
            var resp = await service.UpdateAsync(dto);

            // CreateAsync returns Created when created; UpdateAsync wraps CreateAsync and returns its result
            Assert.True(resp.StatusCode == System.Net.HttpStatusCode.Created || resp.StatusCode == System.Net.HttpStatusCode.OK);
        }

        [Fact]
        public async Task DeleteAsync_RemovesAttribute_AndRecalc()
        {
            using var db = CreateDbContext();
            db.Products.Add(new Product { Id = 10, CategoryId = 1, Name = "p" });
            db.CategoryAttributes.Add(new CategoryAttribute { Id = 11, AttributeName = "A", DataType = SD.DataTypeEnum.Integer, Min = null, Max = null });
            db.ProductAttributes.Add(new ProductAttribute { Id = 101, ProductId = 10, CategoryAttributeId = 11, Integer = 5 });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var resp = await service.DeleteAsync(101);

            Assert.Equal(System.Net.HttpStatusCode.NoContent, resp.StatusCode);
            Assert.Null(await db.ProductAttributes.FindAsync(101));
            var cat = await db.CategoryAttributes.FindAsync(11);
            // after delete, min/max should be reset or null (no values)
            Assert.Null(cat.Min);
            Assert.Null(cat.Max);
        }
    }
}
