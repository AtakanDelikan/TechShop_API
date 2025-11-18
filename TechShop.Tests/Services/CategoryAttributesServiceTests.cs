using Microsoft.EntityFrameworkCore;
using System.Net;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services;
using TechShop_API.Utility;
using Xunit;
using static TechShop_API.Utility.SD;

namespace TechShop.Tests.Services
{
    public class CategoryAttributeServiceTests
    {
        private ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAttributes()
        {
            using var db = CreateDb();
            db.CategoryAttributes.Add(new CategoryAttribute
            {
                AttributeName = "Color",
                CategoryId = 1
            });
            db.SaveChanges();

            var service = new CategoryAttributeService(db);

            var result = await service.GetAllAsync();

            Assert.Single(result);
            Assert.Equal("Color", result.First().AttributeName);
        }

        [Fact]
        public async Task GetCategoryAttributeDetailsAsync_ReturnsNull_WhenNotFound()
        {
            using var db = CreateDb();
            var service = new CategoryAttributeService(db);

            var result = await service.GetCategoryAttributeDetailsAsync(100);

            Assert.Null(result);
        }

        [Fact]
        public async Task CreateAsync_ReturnsBadRequest_WhenCategoryNotFound()
        {
            using var db = CreateDb();
            var service = new CategoryAttributeService(db);

            var dto = new CategoryAttributeCreateDTO
            {
                AttributeName = "Size",
                CategoryId = 99,
                DataType = DataTypeEnum.String
            };

            var result = await service.CreateAsync(dto);

            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task CreateAsync_Succeeds_WhenCategoryExists()
        {
            using var db = CreateDb();
            db.Categories.Add(new Category { Id = 1, Name = "Electronics" });
            db.SaveChanges();

            var service = new CategoryAttributeService(db);

            var dto = new CategoryAttributeCreateDTO
            {
                AttributeName = "Weight",
                CategoryId = 1,
                DataType = DataTypeEnum.Decimal
            };

            var result = await service.CreateAsync(dto);

            Assert.True(result.IsSuccess);
            Assert.Equal(HttpStatusCode.Created, result.StatusCode);

            var attr = Assert.IsType<CategoryAttribute>(result.Result);
            Assert.Equal("Weight", attr.AttributeName);
            Assert.Equal(DataTypeEnum.Decimal, attr.DataType);
        }

        [Fact]
        public async Task UpdateAsync_RemovesProductAttributes_WhenDataTypeChanges()
        {
            using var db = CreateDb();

            db.Categories.Add(new Category { Id = 1, Name = "Electronics" });
            db.CategoryAttributes.Add(new CategoryAttribute
            {
                Id = 1,
                AttributeName = "Power",
                CategoryId = 1,
                DataType = DataTypeEnum.String
            });

            db.ProductAttributes.Add(new ProductAttribute
            {
                ProductId = 1,
                CategoryAttributeId = 1,
                String = "OldValue"
            });

            db.SaveChanges();

            var service = new CategoryAttributeService(db);

            var dto = new CategoryAttributeUpdateDTO
            {
                AttributeName = "Power",
                DataType = DataTypeEnum.Integer
            };

            await service.UpdateAsync(1, dto);

            Assert.Empty(db.ProductAttributes);       // Product attributes deleted
            Assert.Equal(DataTypeEnum.Integer, db.CategoryAttributes.First().DataType);
        }

        [Fact]
        public async Task DeleteAsync_RemovesAttributeCompletely()
        {
            using var db = CreateDb();

            db.CategoryAttributes.Add(new CategoryAttribute
            {
                Id = 10,
                AttributeName = "Voltage",
                CategoryId = 1
            });
            db.SaveChanges();

            var service = new CategoryAttributeService(db);

            var result = await service.DeleteAsync(10);

            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
            Assert.Empty(db.CategoryAttributes);
        }
    }
}
