using System.Collections.Generic;
using System.Linq;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using Xunit;

namespace TechShop_API.Services
{
    public class ProductMappingTests
    {
        [Fact]
        public void ToListItem_ShouldMapProductCorrectly()
        {
            var product = new Product
            {
                Id = 1,
                CategoryId = 2,
                Category = new Category { Name = "Cat1" },
                Name = "Prod1",
                Description = "Desc",
                Price = 10m,
                Stock = 5,
                ProductImages = new List<ProductImage>
                {
                    new ProductImage { Url = "img1", DisplayOrder = 2 },
                    new ProductImage { Url = "img2", DisplayOrder = 1 }
                }
            };

            var dto = ProductMapping.ToListItem(product);

            Assert.Equal(product.Id, dto.Id);
            Assert.Equal(product.CategoryId, dto.CategoryId);
            Assert.Equal(product.Category.Name, dto.CategoryName);
            Assert.Equal(product.Name, dto.Name);
            Assert.Equal(product.Description, dto.Description);
            Assert.Equal(product.Price, dto.Price);
            Assert.Equal(product.Stock, dto.Stock);
            Assert.Equal(new List<string> { "img2", "img1" }, dto.Images);
        }

        [Fact]
        public void ToAttributeDto_ShouldMapBasedOnDataType()
        {
            var stringAttr = new ProductAttribute
            {
                Id = 1,
                CategoryAttribute = new CategoryAttribute { DataType = Utility.SD.DataTypeEnum.String, AttributeName = "StringAttr" },
                String = "value"
            };

            var dto = ProductMapping.ToAttributeDto(stringAttr);

            Assert.Equal("value", dto.Value);
            Assert.Equal("StringAttr", dto.Name);
        }

        [Fact]
        public void ToDetails_ShouldMapProductWithAttributes()
        {
            var product = new Product
            {
                Id = 1,
                CategoryId = 2,
                Category = new Category { Name = "Cat1" },
                Name = "Prod1",
                Description = "Desc",
                Price = 10m,
                Stock = 5,
                ProductImages = new List<ProductImage>()
            };

            var attributes = new List<ProductAttributeDTO>
            {
                new ProductAttributeDTO { Id = 1, Name = "Attr1", Value = "Val1" }
            };

            var dto = ProductMapping.ToDetails(product, attributes);

            Assert.Equal(product.Id, dto.Id);
            Assert.Equal(1, dto.ProductAttributes.Count);
            Assert.Equal("Attr1", dto.ProductAttributes.First().Name);
        }
    }
}
