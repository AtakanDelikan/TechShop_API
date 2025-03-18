using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using CsvHelper;
using System.Globalization;
using CsvHelper.Configuration;
using TechShop_API.Utility;

namespace TechShop_API.Controllers
{
    [Route("api/BulkImport")]
    [ApiController]
    public class BulkImportController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private ApiResponse _response;
        public BulkImportController(ApplicationDbContext db)
        {
            _db = db;
            _response = new ApiResponse();
        }

        [HttpPost("importCategories")]
        public async Task<ActionResult<ApiResponse>> ImportCategories(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Invalid file");
                return BadRequest(_response);
            }

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            });

            var categories = csv.GetRecords<CategoryCsvDTO>().ToList();
            Dictionary<string, int> createdCategories = new();

            foreach (var category in categories)
            {
                int? parentId = null;

                if (!string.IsNullOrEmpty(category.Parent) && createdCategories.TryGetValue(category.Parent, out int parentCategoryId))
                {
                    parentId = parentCategoryId;
                }

                var newCategory = new Category
                {
                    Name = category.Name,
                    Description = category.Description,
                    ParentCategoryId = parentId
                };

                _db.Categories.Add(newCategory);
                await _db.SaveChangesAsync();

                createdCategories[category.Name] = newCategory.Id;
            }

            return _response;
        }

        [HttpPost("importCategoryAttributes")]
        public async Task<ActionResult<ApiResponse>> ImportCategoryAttributes(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Invalid file");
                return BadRequest(_response);
            }

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null // Ignore missing fields
            });

            var categoryAttributes = csv.GetRecords<CategoryAttributeCsvDTO>().ToList();

            foreach (var categoryAttribute in categoryAttributes)
            {
                var category = await _db.Categories.FirstOrDefaultAsync(c => c.Name == categoryAttribute.CategoryName);
                if (category == null)
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add($"Category '{categoryAttribute.CategoryName}' not found.");
                    continue;
                }

                var newCategoryAttribute = new CategoryAttribute
                {
                    CategoryId = category.Id,
                    AttributeName = categoryAttribute.AttributeName,
                    DataType = ParseDataType(categoryAttribute.DataType)
                };

                _db.CategoryAttributes.Add(newCategoryAttribute);
            }

            await _db.SaveChangesAsync();

            return _response;
        }

        private SD.DataTypeEnum ParseDataType(string dataType)
        {
            return dataType.ToLower() switch
            {
                "string" => SD.DataTypeEnum.String,
                "integer" => SD.DataTypeEnum.Integer,
                "decimal" => SD.DataTypeEnum.Decimal,
                "boolean" => SD.DataTypeEnum.Boolean,
                _ => throw new ArgumentException($"Unknown data type: {dataType}")
            };
        }

        [HttpPost("importProducts")]
        public async Task<ActionResult<ApiResponse>> ImportProducts(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Invalid file");
                return BadRequest(_response);
            }

            // Read and parse the CSV file
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ",", // Ensure the delimiter is set to comma
                MissingFieldFound = null // Ignore missing fields
            });

            // Read the CSV file as a list of dynamic objects
            var records = csv.GetRecords<dynamic>().ToList();

            // Find the category in the database
            string categoryName = records[0].Category;
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);
            if (category == null)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages.Add($"Category '{categoryName}' not found.");
                return NotFound(_response);
            }

            // Process each record
            foreach (var record in records)
            {
                // Create the product
                var product = new Product
                {
                    CategoryId = category.Id,
                    Name = record.Name, // Access columns dynamically
                    Description = record.Description,
                    Price = decimal.Parse(record.Price) // Ensure the price is parsed as a decimal
                };

                _db.Products.Add(product);
                await _db.SaveChangesAsync(); // Save to get the ProductId

                if (((IDictionary<string, object>)record).ContainsKey("Images"))
                {
                    string[] images = record.Images.Split('|');

                    // Print the resulting array
                    int displayOrder = 1;
                    foreach (string image in images)
                    {
                        ProductImage productImageToCreate = new ProductImage
                        {
                            ProductId = product.Id,
                            Url = image,
                            DisplayOrder = displayOrder
                        };

                        _db.ProductImages.Add(productImageToCreate);
                        displayOrder++;
                    }

                    _db.SaveChanges();
                }

                // Create product attributes
                var categoryAttributes = await _db.CategoryAttributes
                .Where(ca => ca.CategoryId == category.Id)
                .ToListAsync();

                foreach (var categoryAttribute in categoryAttributes)
                {
                    // Check if the column exists in the CSV file
                    if (((IDictionary<string, object>)record).ContainsKey(categoryAttribute.AttributeName))
                    {
                        var attributeValue = ((IDictionary<string, object>)record)[categoryAttribute.AttributeName]?.ToString();

                        if (!string.IsNullOrEmpty(attributeValue))
                        {
                            var productAttribute = new ProductAttribute
                            {
                                ProductId = product.Id,
                                CategoryAttributeId = categoryAttribute.Id,
                                String = categoryAttribute.DataType == SD.DataTypeEnum.String ? attributeValue : null,
                                Integer = categoryAttribute.DataType == SD.DataTypeEnum.Integer ? int.Parse(attributeValue) : null,
                                Decimal = categoryAttribute.DataType == SD.DataTypeEnum.Decimal ? double.Parse(attributeValue) : null,
                                Boolean = categoryAttribute.DataType == SD.DataTypeEnum.Boolean ? attributeValue == "Yes" : null
                            };

                            _db.ProductAttributes.Add(productAttribute);

                            if (categoryAttribute.DataType == SD.DataTypeEnum.String
                                && !categoryAttribute.UniqueValues.Contains(attributeValue))
                            {
                                categoryAttribute.UniqueValues.Add(attributeValue);
                                _db.CategoryAttributes.Update(categoryAttribute);
                            }

                            if ((categoryAttribute.DataType == SD.DataTypeEnum.Integer
                                || categoryAttribute.DataType == SD.DataTypeEnum.Decimal)
                                && double.TryParse(attributeValue, out double result))
                            {
                                if (!categoryAttribute.Min.HasValue)
                                {
                                    categoryAttribute.Min = result;
                                    categoryAttribute.Max = result;
                                }
                                if (categoryAttribute.Min > result)
                                {
                                    categoryAttribute.Min = result;
                                }
                                if (categoryAttribute.Max < result)
                                {
                                    categoryAttribute.Max = result;
                                }
                                _db.CategoryAttributes.Update(categoryAttribute);
                            }
                        }
                    }
                }

                await _db.SaveChangesAsync();
            }

            return _response;
        }

    }
}
