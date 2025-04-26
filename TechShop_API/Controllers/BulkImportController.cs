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
using Microsoft.AspNetCore.Identity;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace TechShop_API.Controllers
{
    [Route("api/BulkImport")]
    [ApiController]
    public class BulkImportController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private ApiResponse _response;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public BulkImportController(ApplicationDbContext db,
            UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _response = new ApiResponse();
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpPost("importCategories")]
        [Authorize(Roles = SD.Role_Admin)]
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
        [Authorize(Roles = SD.Role_Admin)]
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
        [Authorize(Roles = SD.Role_Admin)]
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

        [HttpPost("createHunderedUsers")]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<ActionResult<ApiResponse>> createHunderedUsers()
        {
            const string password = "12345";
            try
            {

                for (int i = 0; i < 99; i++)
                {
                    string userName = $"user{i + 1}";

                    ApplicationUser newUser = new()
                    {
                        UserName = userName,
                        Email = userName,
                        NormalizedEmail = userName.ToUpper(),
                        Name = userName,
                    };

                    var result = await _userManager.CreateAsync(newUser, password);
                    if (result.Succeeded)
                    {
                        if (!_roleManager.RoleExistsAsync(SD.Role_Admin).GetAwaiter().GetResult())
                        {
                            await _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin));
                            await _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer));
                        }
                        await _userManager.AddToRoleAsync(newUser, SD.Role_Customer);
                    }
                }


                ApplicationUser adminUser = new()
                {
                    UserName = "admin",
                    Email = "admin",
                    NormalizedEmail = "admin".ToUpper(),
                    Name = "admin",
                };
                var resultAdmin = await _userManager.CreateAsync(adminUser, password);
                if (resultAdmin.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, SD.Role_Admin);
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
                
            }
            catch (Exception ex)
            {

            }
            _response.StatusCode = HttpStatusCode.BadRequest;
            _response.IsSuccess = false;
            _response.ErrorMessages.Add("Error while registering");
            return BadRequest(_response);
        }

        static int GetWeightedRandom(Dictionary<int, int> items)
        {
            int totalWeight = items.Values.Sum();
            int randomValue = new Random().Next(1, totalWeight + 1); // Random value between 1 and total weight

            int cumulative = 0;
            foreach (var item in items)
            {
                cumulative += item.Value;
                if (randomValue <= cumulative)
                {
                    return item.Key; // Select this number
                }
            }

            return -1; // Should never happen
        }

        static int[] GetUniqueRandomNumbers(int n, int x)
        {
            // Gets n unique random numbers from 0-x
            if (n > x + 1) throw new ArgumentException("n cannot be larger than x+1");

            int[] numbers = new int[x + 1];
            for (int i = 0; i <= x; i++) numbers[i] = i; // Fill array with 0 to x

            Random random = new Random();
            for (int i = numbers.Length - 1; i > 0; i--)
            {
                int j = random.Next(0, i + 1);
                (numbers[i], numbers[j]) = (numbers[j], numbers[i]); // Swap
            }

            return numbers[..n]; // Return first 'n' elements
        }

        static DateTime GetRandomDate2024()
        {
            Random random = new Random();
            DateTime start = new DateTime(2024, 1, 1);
            DateTime end = new DateTime(2024, 12, 31, 23, 59, 59);

            int range = (int)(end - start).TotalSeconds; // Total seconds in 2024
            int randomSeconds = random.Next(0, range);   // Pick random second
            return start.AddSeconds(randomSeconds);      // Add to start date
        }

        private async Task createRandomOrder()
        {
            int userCount = await _db.ApplicationUsers.CountAsync();
            if (userCount == 0) return; // No users in DB


            int randomUserIndex = new Random().Next(0, userCount - 1);
            var randomUser = await _db.ApplicationUsers.Skip(randomUserIndex).FirstOrDefaultAsync();

            Dictionary<int, int> uniqueProductWeights = new Dictionary<int, int>
            {
                { 1, 78 }, { 2, 87 }, { 3, 94 }, { 4, 98 }, { 5, 100 },
                { 6, 98 }, { 7, 94 }, { 8, 87 }, { 9, 78 }, { 10, 69 },
                { 11, 58 }, { 12, 48 }, { 13, 39 }
            };
            int uniqueProductCount = GetWeightedRandom(uniqueProductWeights);
            int productCount = await _db.Products.CountAsync();
            if (productCount < uniqueProductCount) return; // Not enough products in DB

            OrderHeader order = new()
            {
                ApplicationUserId = randomUser.Id,
                PickupEmail = randomUser.Email,
                PickupName = randomUser.Name,
                PickupPhoneNumber = "",
                OrderTotal = 0, // Updated at the end
                OrderDate = GetRandomDate2024(), // A random time in 2024
                StripePaymentIntentID = "",
                TotalItems = 0, // Updated at the end
                Status = SD.status_delivered
            };

            _db.OrderHeaders.Add(order);
            _db.SaveChanges();

            Dictionary<int, int> productCountWeights = new Dictionary<int, int>
            {
                { 1, 80 }, { 2, 45 }, { 3, 10 }, { 4, 5 }, { 5, 2 },{ 6, 1 }
            };

            int[] uniqueRandomProducts = GetUniqueRandomNumbers(uniqueProductCount, productCount);

            int totalItems = 0;
            decimal OrdersTotal = 0;
            foreach (int productIndex in uniqueRandomProducts)
            {
                var randomProduct = await _db.Products.Skip(productIndex).FirstOrDefaultAsync();
                int randomProductQuantity = GetWeightedRandom(productCountWeights);

                OrderDetail orderDetail = new()
                {
                    OrderHeaderId = order.OrderHeaderId,
                    ItemName = randomProduct.Name,
                    ProductId = randomProduct.Id,
                    Price = randomProduct.Price,
                    Quantity = randomProductQuantity,
                };
                totalItems += randomProductQuantity;
                OrdersTotal += randomProduct.Price * randomProductQuantity;
                _db.OrderDetails.Add(orderDetail);
            }

            order.TotalItems = totalItems;
            order.OrderTotal = OrdersTotal;
            _db.OrderHeaders.Update(order);

            _db.SaveChanges();
        }

        [HttpPost("createRandomOrders")]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<ActionResult<ApiResponse>> createRandomOrders(int randomOrderCount = 1)
        {
            for (int i = 0; i < randomOrderCount; i++)
            {
                await createRandomOrder();
            }

            return _response;
        }

    }
}
