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

        private async Task<ActionResult<ApiResponse>> ImportProductsFromFile(IFormFile file)
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
                    Price = decimal.Parse(record.Price), // Ensure the price is parsed as a decimal
                    Stock = 1000
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

        [HttpPost("importProducts")]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<ActionResult<ApiResponse>> ImportProductsFromOneFile(IFormFile file)
        {
            try
            {
                await ImportProductsFromFile(file);
            }
            catch (Exception ex) {
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Invalid file");
                return BadRequest(_response);
            }
            _response.IsSuccess = true;
            return Ok(_response);
        }

        [HttpPost("importMultipleProducts")]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<ActionResult<ApiResponse>> ImportProductsFromMultipleFile([FromForm] List<IFormFile> files)
        {
            try
            {
                
                foreach (var file in files)
                {
                    await ImportProductsFromFile(file);
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Invalid file");
                return BadRequest(_response);
            }
            _response.IsSuccess = true;
            return Ok(_response);
        }

        [HttpPost("createHunderedUsers")]
        //[Authorize(Roles = SD.Role_Admin)]
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

        static DateTime GetRandomDate(int year)
        {
            Random random = new Random();
            DateTime start = new DateTime(year, 1, 1);
            DateTime end = new DateTime(year, 12, 31, 23, 59, 59);

            int range = (int)(end - start).TotalSeconds; // Total seconds in year
            int randomSeconds = random.Next(0, range);   // Pick random second
            return start.AddSeconds(randomSeconds);      // Add to start date
        }

        private async Task createRandomOrder(int year=2024)
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

            DateTime orderDate = GetRandomDate(year);

            OrderHeader order = new()
            {
                ApplicationUserId = randomUser.Id,
                PickupEmail = randomUser.Email,
                PickupName = randomUser.Name,
                PickupPhoneNumber = "",
                OrderTotal = 0, // Updated at the end
                OrderDate = orderDate, // A random time
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

            int[] uniqueRandomProducts = GetUniqueRandomNumbers(uniqueProductCount, productCount-1);

            int totalItems = 0;
            decimal OrdersTotal = 0;
            foreach (int productIndex in uniqueRandomProducts)
            {
                var randomProduct = await _db.Products.Skip(productIndex).FirstOrDefaultAsync();
                int randomProductQuantity = GetWeightedRandom(productCountWeights);

                await createRandomReview(randomUser, randomProduct, orderDate);

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

                randomProduct.Stock -= randomProductQuantity;
                _db.Products.Update(randomProduct);
            }

            order.TotalItems = totalItems;
            order.OrderTotal = OrdersTotal;
            _db.OrderHeaders.Update(order);

            _db.SaveChanges();
        }

        private class Review
        {
            public int Rating { get; set; }
            public string Content { get; set; }
        }

        static Review GetRandomReview(List<Review> reviews)
        {
            Random rand = new Random();
            int index = rand.Next(reviews.Count);
            return reviews[index];
        }

        private async Task createRandomReview(ApplicationUser user, Product product, DateTime time)
        {
            // assumes users leave rating %15 of the time, probably the worst solution :)
            Random rand = new Random();
            int chance = rand.Next(1, 101);
            if (chance <= 15) // 15% chance (1-105)
            {
                return;
            }

            List<Review> mockReviews = new List<Review>
            {
            // 5 Stars
            new Review { Rating = 5, Content = "Absolutely love this product! Highly recommend it." },
            new Review { Rating = 5, Content = "Exceeded my expectations in every way!" },
            new Review { Rating = 5, Content = "Fantastic quality and easy to use." },
            new Review { Rating = 5, Content = "Perfect! Will definitely buy again." },
            new Review { Rating = 5, Content = "Top-notch, very satisfied with the purchase." },
            new Review { Rating = 5, Content = "Amazing product, works better than advertised!" },
            new Review { Rating = 5, Content = "Five stars, couldn’t be happier!" },
            new Review { Rating = 5, Content = "Incredible performance, worth every penny." },
            new Review { Rating = 5, Content = "Highly functional and looks great too!" },
            new Review { Rating = 5, Content = "Outstanding value for the price!" },
            // 4 Stars
            new Review { Rating = 4, Content = "Very good overall, just a few minor flaws." },
            new Review { Rating = 4, Content = "Works well, would recommend to others." },
            new Review { Rating = 4, Content = "Good product, packaging could be better." },
            new Review { Rating = 4, Content = "Happy with the purchase, does its job." },
            new Review { Rating = 4, Content = "Solid product, missing a few features I wanted." },
            new Review { Rating = 4, Content = "Pretty close to perfect, just a little pricey." },
            new Review { Rating = 4, Content = "Love it, but instructions were a bit unclear." },
            new Review { Rating = 4, Content = "Build quality is great, minor issues with delivery." },
            new Review { Rating = 4, Content = "Excellent choice, just wish it came in more colors." },
            new Review { Rating = 4, Content = "Performs very well, minor setup challenges." },
            // 3 Stars
            new Review { Rating = 3, Content = "It's okay, does what it says." },
            new Review { Rating = 3, Content = "Average performance, nothing special." },
            new Review { Rating = 3, Content = "Not bad, but there are better options." },
            new Review { Rating = 3, Content = "Meh, it's acceptable for the price." },
            new Review { Rating = 3, Content = "Some good, some bad — middle of the road." },
            new Review { Rating = 3, Content = "It works, but not very impressed." },
            new Review { Rating = 3, Content = "Decent product, questionable durability." },
            new Review { Rating = 3, Content = "Functional, but a bit disappointing." },
            new Review { Rating = 3, Content = "Not terrible, but would not buy again." },
            new Review { Rating = 3, Content = "It's fine for temporary use." },
            // 2 Stars
            new Review { Rating = 2, Content = "Disappointed, not as described." },
            new Review { Rating = 2, Content = "Poor build quality, expected better." },
            new Review { Rating = 2, Content = "Worked at first, then broke quickly." },
            new Review { Rating = 2, Content = "Customer service was helpful, but product wasn't." },
            new Review { Rating = 2, Content = "Not reliable, had to replace it." },
            new Review { Rating = 2, Content = "Below average, not worth the money." },
            new Review { Rating = 2, Content = "Subpar experience overall." },
            new Review { Rating = 2, Content = "Does not live up to the hype." },
            new Review { Rating = 2, Content = "Cheap materials, not impressed." },
            new Review { Rating = 2, Content = "Would not recommend to others." },
            // 1 Star
            new Review { Rating = 1, Content = "Terrible quality, broke immediately." },
            new Review { Rating = 1, Content = "Waste of money, stay away." },
            new Review { Rating = 1, Content = "Very disappointed, won't buy again." },
            new Review { Rating = 1, Content = "Item was defective on arrival." },
            new Review { Rating = 1, Content = "Worst product I have ever purchased." },
            new Review { Rating = 1, Content = "Does not work at all!" },
            new Review { Rating = 1, Content = "Scammy product, avoid!" },
            new Review { Rating = 1, Content = "Completely unusable, very upset." },
            new Review { Rating = 1, Content = "Nothing good to say about it." },
            new Review { Rating = 1, Content = "Should be removed from the market." }
            };

            Review randomReview = GetRandomReview(mockReviews);
            
            Comment comment = new Comment
            {
                ProductId = product.Id,
                ApplicationUserId = user.Id,
                Content = randomReview.Content,
                Rating = randomReview.Rating,
                CreatedAt = time,
            };

            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();
        }

        private async Task recalculateRatingsForAllProducts()
        {
            var products = _db.Products.ToList();

            // Loop through each product and recalculate its rating
            foreach (var product in products)
            {
                var ratingsQuery = _db.Comments
                    .Where(c => c.ProductId == product.Id)
                    .Select(c => c.Rating);

                double averageRating = ratingsQuery.Any()
                    ? ratingsQuery.Average()
                    : 0;

                product.Rating = averageRating;
                _db.Products.Update(product);
            }

            // Save changes for all products at once
            await _db.SaveChangesAsync();
        }

        [HttpPost("createRandomOrders")]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<ActionResult<ApiResponse>> createRandomOrders()
        {
            for (int i = 0; i < 684; i++)
            {
                await createRandomOrder(2021);
            }
            for (int i = 0; i < 912; i++)
            {
                await createRandomOrder(2022);
            }
            for (int i = 0; i < 1085; i++)
            {
                await createRandomOrder(2023);
            }
            for (int i = 0; i < 1243; i++)
            {
                await createRandomOrder(2024);
            }
            await recalculateRatingsForAllProducts();

            return _response;
        }

    }
}
