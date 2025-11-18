using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Utility;
using System.Globalization;

namespace TechShop_API.Services
{
    public static class ProductMapping
    {
        public static ProductListItemDTO ToListItem(Product p)
        {
            return new ProductListItemDTO
            {
                Id = p.Id,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? string.Empty,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Stock = p.Stock,
                Images = p.ProductImages?.OrderBy(pi => pi.DisplayOrder).Select(pi => pi.Url).ToList() ?? new List<string>()
            };
        }

        public static ProductAttributeDTO ToAttributeDto(ProductAttribute pa)
        {
            string? value = pa.CategoryAttribute?.DataType switch
            {
                SD.DataTypeEnum.String => pa.String,
                SD.DataTypeEnum.Integer => pa.Integer?.ToString(),
                SD.DataTypeEnum.Decimal => pa.Decimal?.ToString("F2", CultureInfo.InvariantCulture),
                SD.DataTypeEnum.Boolean => pa.Boolean?.ToString().ToLower(),
                _ => null
            };

            return new ProductAttributeDTO
            {
                Id = pa.Id,
                CategoryAttributeId = pa.CategoryAttributeId,
                Name = pa.CategoryAttribute?.AttributeName ?? string.Empty,
                Value = value
            };
        }

        public static ProductDetailsDTO ToDetails(Product p, IEnumerable<ProductAttributeDTO> attributes)
        {
            return new ProductDetailsDTO
            {
                Id = p.Id,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? string.Empty,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Stock = p.Stock,
                Images = p.ProductImages?.OrderBy(pi => pi.DisplayOrder).Select(pi => pi.Url).ToList() ?? new List<string>(),
                ProductAttributes = attributes?.ToList() ?? new List<ProductAttributeDTO>()
            };
        }
    }
}
