using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Utility;
using TechShop_API.Services.Interfaces;

namespace TechShop_API.Services
{
    public class BulkImportService : IBulkImportService
    {
        private readonly ApplicationDbContext _db;

        public BulkImportService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ApiResponse> ImportCategoriesAsync(Stream fileStream)
        {
            using var reader = new StreamReader(fileStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });

            var records = csv.GetRecords<CategoryCsvDTO>().ToList();
            var newCategories = new List<Category>();
            var dictionaryMap = new Dictionary<string, Category>();

            // Pass 1: Create all categories without parent relationships
            foreach (var record in records)
            {
                var category = new Category
                {
                    Name = record.Name,
                    Description = record.Description
                };
                newCategories.Add(category);
                dictionaryMap[record.Name] = category;
            }

            // Pass 2: Establish Parent Relationships in memory
            foreach (var record in records)
            {
                if (!string.IsNullOrEmpty(record.Parent) && dictionaryMap.TryGetValue(record.Parent, out var parentCategory))
                {
                    dictionaryMap[record.Name].ParentCategory = parentCategory;
                }
            }

            _db.Categories.AddRange(newCategories);
            await _db.SaveChangesAsync();

            return new ApiResponse { IsSuccess = true };
        }

        public async Task<ApiResponse> ImportCategoryAttributesAsync(Stream fileStream)
        {
            using var reader = new StreamReader(fileStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });

            var records = csv.GetRecords<CategoryAttributeCsvDTO>().ToList();
            var categoryNames = records.Select(r => r.CategoryName).Distinct().ToList();

            var existingCategories = await _db.Categories
                .Where(c => categoryNames.Contains(c.Name))
                .ToDictionaryAsync(c => c.Name, c => c.Id);

            var newAttributes = new List<CategoryAttribute>();
            var response = new ApiResponse { IsSuccess = true };

            foreach (var record in records)
            {
                if (!existingCategories.TryGetValue(record.CategoryName, out var categoryId))
                {
                    response.ErrorMessages.Add($"Skipped attribute '{record.AttributeName}': Category '{record.CategoryName}' not found.");
                    continue;
                }

                newAttributes.Add(new CategoryAttribute
                {
                    CategoryId = categoryId,
                    AttributeName = record.AttributeName,
                    DataType = ParseDataType(record.DataType)
                });
            }

            _db.CategoryAttributes.AddRange(newAttributes);
            await _db.SaveChangesAsync();

            return response;
        }

        public async Task<ApiResponse> ImportProductsAsync(Stream fileStream)
        {
            var response = new ApiResponse { IsSuccess = true };

            var records = GetCsvRecords(fileStream);
            if (!records.Any()) return response;

            var category = await GetCategoryAsync(records.First());
            if (category == null)
            {
                response.IsSuccess = false;
                response.ErrorMessages.Add("Category not found.");
                return response;
            }

            var categoryAttributes = await _db.CategoryAttributes
                .Where(ca => ca.CategoryId == category.Id)
                .ToListAsync();

            var newProducts = new List<Product>();

            foreach (var record in records)
            {
                var row = record;

                var product = CreateBaseProduct(row, category.Id);
                product.ProductImages = ParseImages(row);
                product.ProductAttributes = ParseAttributes(row, categoryAttributes);

                newProducts.Add(product);
            }

            _db.Products.AddRange(newProducts);
            await _db.SaveChangesAsync();

            return response;
        }

        // --- HELPER METHODS ---

        private List<IDictionary<string, object>> GetCsvRecords(Stream fileStream)
        {
            using var reader = new StreamReader(fileStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null
            });

            return csv.GetRecords<dynamic>()
                      .Cast<IDictionary<string, object>>()
                      .ToList();
        }

        private async Task<Category> GetCategoryAsync(IDictionary<string, object> firstRecord)
        {
            // Safely check if "Category" exists in the dictionary
            if (firstRecord.TryGetValue("Category", out var categoryObj) && categoryObj != null)
            {
                string categoryName = categoryObj.ToString();
                return await _db.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);
            }

            return null;
        }

        private Product CreateBaseProduct(IDictionary<string, object> row, int categoryId)
        {
            return new Product
            {
                CategoryId = categoryId,
                Name = row.TryGetValue("Name", out var nameVal) ? nameVal?.ToString() : "Unknown",
                Description = row.TryGetValue("Description", out var descVal) ? descVal?.ToString() : "",
                Price = row.TryGetValue("Price", out var priceVal) && decimal.TryParse(priceVal?.ToString(), out var price) ? price : 0,
                Stock = 1000
            };
        }

        private List<ProductImage> ParseImages(IDictionary<string, object> row)
        {
            var imagesList = new List<ProductImage>();

            if (row.TryGetValue("Images", out var imagesVal) && imagesVal != null)
            {
                var images = imagesVal.ToString().Split('|');
                for (int i = 0; i < images.Length; i++)
                {
                    imagesList.Add(new ProductImage { Url = images[i], DisplayOrder = i + 1 });
                }
            }
            return imagesList;
        }

        private List<ProductAttribute> ParseAttributes(IDictionary<string, object> row, List<CategoryAttribute> categoryAttributes)
        {
            var attributesList = new List<ProductAttribute>();

            foreach (var attr in categoryAttributes)
            {
                if (!row.TryGetValue(attr.AttributeName, out var attrValObj) || attrValObj == null)
                    continue;

                var attrStr = attrValObj.ToString();
                if (string.IsNullOrWhiteSpace(attrStr))
                    continue;

                var prodAttr = new ProductAttribute { CategoryAttributeId = attr.Id };
                ApplyAttributeValue(prodAttr, attr, attrStr);
                attributesList.Add(prodAttr);
            }

            return attributesList;
        }

        private void ApplyAttributeValue(ProductAttribute prodAttr, CategoryAttribute attr, string attrStr)
        {
            switch (attr.DataType)
            {
                case SD.DataTypeEnum.String:
                    prodAttr.String = attrStr;
                    if (!attr.UniqueValues.Contains(attrStr)) attr.UniqueValues.Add(attrStr);
                    break;

                case SD.DataTypeEnum.Integer:
                    if (int.TryParse(attrStr, out int intVal))
                    {
                        prodAttr.Integer = intVal;
                        UpdateMinMax(attr, intVal);
                    }
                    break;

                case SD.DataTypeEnum.Decimal:
                    if (double.TryParse(attrStr, out double decVal))
                    {
                        prodAttr.Decimal = decVal;
                        UpdateMinMax(attr, decVal);
                    }
                    break;

                case SD.DataTypeEnum.Boolean:
                    prodAttr.Boolean = attrStr.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
                                       attrStr.Equals("True", StringComparison.OrdinalIgnoreCase);
                    break;
            }
        }

        private void UpdateMinMax(CategoryAttribute attr, double value)
        {
            if (!attr.Min.HasValue) attr.Min = value;
            if (!attr.Max.HasValue) attr.Max = value;
            if (attr.Min > value) attr.Min = value;
            if (attr.Max < value) attr.Max = value;
        }

        private SD.DataTypeEnum ParseDataType(string dataType) => dataType.ToLower() switch
        {
            "string" => SD.DataTypeEnum.String,
            "integer" => SD.DataTypeEnum.Integer,
            "decimal" => SD.DataTypeEnum.Decimal,
            "boolean" => SD.DataTypeEnum.Boolean,
            _ => throw new ArgumentException($"Unknown data type: {dataType}")
        };
    }
}