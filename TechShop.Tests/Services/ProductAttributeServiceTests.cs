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

        // --- GET ALL & GET BY PRODUCT TESTS ---

        [Fact]
        public async Task GetAllAsync_ReturnsAllAttributes()
        {
            using var db = CreateDbContext();
            db.Products.Add(new Product { Id = 1, CategoryId = 1, Name = "P1" });
            db.CategoryAttributes.Add(new CategoryAttribute { Id = 1, AttributeName = "A1", DataType = SD.DataTypeEnum.String });
            db.ProductAttributes.Add(new ProductAttribute { Id = 1, ProductId = 1, CategoryAttributeId = 1, String = "Val" });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var resp = await service.GetAllAsync();

            Assert.Equal(System.Net.HttpStatusCode.OK, resp.StatusCode);
            var list = Assert.IsAssignableFrom<System.Collections.Generic.List<ProductAttributeResponseDTO>>(resp.Result);
            Assert.Single(list);
        }

        [Fact]
        public async Task GetByProductAsync_ReturnsNotFound_WhenProductMissing()
        {
            using var db = CreateDbContext();
            var service = CreateService(db);

            var resp = await service.GetByProductAsync(999);

            Assert.False(resp.IsSuccess);
            Assert.Equal(System.Net.HttpStatusCode.NotFound, resp.StatusCode);
            Assert.Contains("Product not found", resp.ErrorMessages);
        }

        [Fact]
        public async Task GetByProductAsync_ReturnsAttributes_WhenProductExists()
        {
            using var db = CreateDbContext();
            db.Products.Add(new Product { Id = 2, CategoryId = 1, Name = "P2" });
            db.CategoryAttributes.Add(new CategoryAttribute { Id = 2, AttributeName = "A2", DataType = SD.DataTypeEnum.Integer });
            db.ProductAttributes.Add(new ProductAttribute { Id = 2, ProductId = 2, CategoryAttributeId = 2, Integer = 10 });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var resp = await service.GetByProductAsync(2);

            Assert.Equal(System.Net.HttpStatusCode.OK, resp.StatusCode);
            var list = Assert.IsAssignableFrom<System.Collections.Generic.List<ProductAttributeResponseDTO>>(resp.Result);
            Assert.Single(list);
        }

        // --- CREATE TESTS (OVERWRITE BRANCH & DATA TYPES) ---

        [Fact]
        public async Task CreateAsync_UpdatesExisting_WhenAttributeAlreadyExists()
        {
            // Tests the branch: if (existing != null) -> reuse update logic
            using var db = CreateDbContext();
            db.Products.Add(new Product { Id = 3, CategoryId = 1, Name = "P3" });
            db.CategoryAttributes.Add(new CategoryAttribute { Id = 3, AttributeName = "A3", DataType = SD.DataTypeEnum.String, UniqueValues = new System.Collections.Generic.List<string> { "old" } });
            db.ProductAttributes.Add(new ProductAttribute { Id = 3, ProductId = 3, CategoryAttributeId = 3, String = "old" });
            await db.SaveChangesAsync();

            var service = CreateService(db);

            var dto = new ProductAttributeCreateDTO { ProductId = 3, CategoryAttributeId = 3, Value = "new" };
            var resp = await service.CreateAsync(dto);

            Assert.Equal(System.Net.HttpStatusCode.OK, resp.StatusCode); // Returns OK instead of Created

            var updatedAttr = await db.ProductAttributes.FindAsync(3);
            Assert.Equal("new", updatedAttr.String);

            // Stats should have recalculated to include "new"
            var catAttr = await db.CategoryAttributes.FindAsync(3);
            Assert.Contains("new", catAttr.UniqueValues);
        }

        [Fact]
        public async Task CreateAsync_UpdatesDecimalStatsCorrectly()
        {
            using var db = CreateDbContext();
            db.Products.Add(new Product { Id = 4, CategoryId = 1, Name = "P4" });
            db.CategoryAttributes.Add(new CategoryAttribute { Id = 4, AttributeName = "Weight", DataType = SD.DataTypeEnum.Decimal });
            await db.SaveChangesAsync();

            var service = CreateService(db);

            await service.CreateAsync(new ProductAttributeCreateDTO { ProductId = 4, CategoryAttributeId = 4, Value = "10.5" });

            var catAttr = await db.CategoryAttributes.FindAsync(4);
            Assert.Equal(10.5, catAttr.Min);
            Assert.Equal(10.5, catAttr.Max);
        }

        // --- UPDATE TESTS (ERROR PATHS & RECALCULATION) ---

        [Fact]
        public async Task UpdateAsync_ReturnsBadRequest_WhenProductMissing()
        {
            using var db = CreateDbContext();
            var service = CreateService(db);

            var dto = new ProductAttributeUpdateDTO { ProductId = 999, CategoryAttributeId = 1, Value = "v" };
            var resp = await service.UpdateAsync(dto);

            Assert.False(resp.IsSuccess);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsBadRequest_WhenCategoryAttributeMissing()
        {
            using var db = CreateDbContext();
            db.Products.Add(new Product { Id = 5, CategoryId = 1, Name = "P5" });
            await db.SaveChangesAsync();

            var service = CreateService(db);

            var dto = new ProductAttributeUpdateDTO { ProductId = 5, CategoryAttributeId = 999, Value = "v" };
            var resp = await service.UpdateAsync(dto);

            Assert.False(resp.IsSuccess);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesValue_AndRecalculatesStats()
        {
            // Verifies the existing record is modified and stats are rebuilt
            using var db = CreateDbContext();
            db.Products.Add(new Product { Id = 6, CategoryId = 1, Name = "P6" });
            db.Products.Add(new Product { Id = 7, CategoryId = 1, Name = "P7" });
            db.CategoryAttributes.Add(new CategoryAttribute { Id = 5, AttributeName = "Size", DataType = SD.DataTypeEnum.Integer, Min = 5, Max = 10 });
            db.ProductAttributes.Add(new ProductAttribute { Id = 4, ProductId = 6, CategoryAttributeId = 5, Integer = 5 });
            db.ProductAttributes.Add(new ProductAttribute { Id = 5, ProductId = 7, CategoryAttributeId = 5, Integer = 10 });
            await db.SaveChangesAsync();

            var service = CreateService(db);

            // Change the max value from 10 to 20
            var dto = new ProductAttributeUpdateDTO { ProductId = 7, CategoryAttributeId = 5, Value = "20" };
            var resp = await service.UpdateAsync(dto);

            Assert.Equal(System.Net.HttpStatusCode.NoContent, resp.StatusCode);

            var catAttr = await db.CategoryAttributes.FindAsync(5);
            Assert.Equal(5, catAttr.Min);
            Assert.Equal(20, catAttr.Max); // Recalculated correctly
        }

        // --- DELETE TESTS ---

        [Fact]
        public async Task DeleteAsync_ReturnsBadRequest_WhenAttributeMissing()
        {
            using var db = CreateDbContext();
            var service = CreateService(db);

            var resp = await service.DeleteAsync(999);

            Assert.False(resp.IsSuccess);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resp.StatusCode);
            Assert.Contains("Product attribute not found", resp.ErrorMessages);
        }

        [Fact]
        public async Task DeleteAsync_RecalculatesStats_WithRemainingItems()
        {
            // Verify that deleting 1 item updates the max bounds based on what is left
            using var db = CreateDbContext();
            db.Products.Add(new Product { Id = 8, CategoryId = 1, Name = "P8" });
            db.Products.Add(new Product { Id = 9, CategoryId = 1, Name = "P9" });
            db.CategoryAttributes.Add(new CategoryAttribute { Id = 6, AttributeName = "Len", DataType = SD.DataTypeEnum.Integer, Min = 1, Max = 50 });
            db.ProductAttributes.Add(new ProductAttribute { Id = 6, ProductId = 8, CategoryAttributeId = 6, Integer = 1 });
            db.ProductAttributes.Add(new ProductAttribute { Id = 7, ProductId = 9, CategoryAttributeId = 6, Integer = 50 });
            await db.SaveChangesAsync();

            var service = CreateService(db);

            // Delete the item causing the Max to be 50
            await service.DeleteAsync(7);

            var catAttr = await db.CategoryAttributes.FindAsync(6);
            Assert.Equal(1, catAttr.Min);
            Assert.Equal(1, catAttr.Max); // Max shifted down to the only remaining item
        }
    }
}
