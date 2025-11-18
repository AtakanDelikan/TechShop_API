using Microsoft.EntityFrameworkCore;
using System.Globalization;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Utility;
using TechShop_API.Services.Interfaces;

namespace TechShop_API.Services
{
    public class ProductFilterService : IProductFilterService
    {
        private readonly ApplicationDbContext _db;

        public ProductFilterService(ApplicationDbContext db)
        {
            _db = db;
        }

        public IQueryable<Product> ApplyFilters(IQueryable<Product> source, Dictionary<string, string> filters)
        {
            if (filters == null || filters.Count == 0) return source;

            IQueryable<Product> query = source;

            // category filter
            if (filters.TryGetValue("category", out var sCategory) && int.TryParse(sCategory, out var categoryId) && categoryId != 0)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            // price range filter - expects "min﹐max"
            if (filters.TryGetValue("price", out var sPrice) && !string.IsNullOrWhiteSpace(sPrice))
            {
                var parts = sPrice.Split('﹐');
                if (parts.Length == 2
                    && decimal.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var min)
                    && decimal.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var max)
                    && min <= max)
                {
                    query = query.Where(p => p.Price >= min && p.Price <= max);
                }
            }

            // attribute filters (custom format like "1[val﹐val];2[min﹐max]")
            if (filters.TryGetValue("attributes", out var attributeFilters) && !string.IsNullOrWhiteSpace(attributeFilters))
            {
                query = ApplyAttributeFilters(query, attributeFilters);
            }

            // search filter (name/description or product attribute string)
            if (filters.TryGetValue("search", out var searchTerm) && !string.IsNullOrWhiteSpace(searchTerm))
            {
                // attributes string match (only string attribute values considered here)
                var productIdsFromAttrs = _db.ProductAttributes
                    .Where(pa => pa.String != null && pa.String.Contains(searchTerm))
                    .Select(pa => pa.ProductId);

                query = query.Where(p => p.Name.Contains(searchTerm) ||
                                         p.Description.Contains(searchTerm) ||
                                         productIdsFromAttrs.Contains(p.Id));
            }

            return query;
        }

        private IQueryable<Product> ApplyAttributeFilters(IQueryable<Product> productsQuery, string attributeFilters)
        {
            // Example attributeFilters: "1[val1﹐val2];2[10﹐20];3[true]"
            var parts = attributeFilters.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var idx = part.IndexOf('[');
                if (idx <= 0 || !part.EndsWith("]")) continue;

                var keyPart = part.Substring(0, idx);
                if (!int.TryParse(keyPart, out var attributeId)) continue;

                var inner = part.Substring(idx + 1, part.Length - idx - 2); // between [ and ]
                var values = inner.Split('﹐', StringSplitOptions.RemoveEmptyEntries);

                var categoryAttribute = _db.CategoryAttributes.FirstOrDefault(ca => ca.Id == attributeId);
                if (categoryAttribute == null) continue;

                IQueryable<ProductAttribute> paQuery = _db.ProductAttributes.Where(pa => pa.CategoryAttributeId == attributeId);

                paQuery = categoryAttribute.DataType switch
                {
                    SD.DataTypeEnum.String => FilterString(paQuery, values),
                    SD.DataTypeEnum.Integer => FilterInteger(paQuery, values),
                    SD.DataTypeEnum.Decimal => FilterDecimal(paQuery, values),
                    SD.DataTypeEnum.Boolean => FilterBoolean(paQuery, values),
                    _ => paQuery
                };

                productsQuery = productsQuery.Where(p => paQuery.Any(pa => pa.ProductId == p.Id));
            }

            return productsQuery;
        }

        private IQueryable<ProductAttribute> FilterString(IQueryable<ProductAttribute> q, string[] values)
        {
            if (values.Length == 0) return q;
            return q.Where(pa => values.Contains(pa.String));
        }

        private IQueryable<ProductAttribute> FilterInteger(IQueryable<ProductAttribute> q, string[] values)
        {
            // expects two values min﹐max
            if (values.Length != 2) return q;
            if (!int.TryParse(values[0], out var min) || !int.TryParse(values[1], out var max)) return q;
            return q.Where(pa => pa.Integer.HasValue && pa.Integer >= min && pa.Integer <= max);
        }

        private IQueryable<ProductAttribute> FilterDecimal(IQueryable<ProductAttribute> q, string[] values)
        {
            if (values.Length != 2) return q;
            if (!double.TryParse(values[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var min) ||
                !double.TryParse(values[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var max)) return q;
            return q.Where(pa => pa.Decimal.HasValue && pa.Decimal >= min && pa.Decimal <= max);
        }

        private IQueryable<ProductAttribute> FilterBoolean(IQueryable<ProductAttribute> q, string[] values)
        {
            if (values.Length != 1) return q;
            if (values[0].Equals("true", StringComparison.OrdinalIgnoreCase))
                return q.Where(pa => pa.Boolean.HasValue && pa.Boolean == true);
            if (values[0].Equals("false", StringComparison.OrdinalIgnoreCase))
                return q.Where(pa => pa.Boolean.HasValue && pa.Boolean == false);
            return q;
        }
    }
}
