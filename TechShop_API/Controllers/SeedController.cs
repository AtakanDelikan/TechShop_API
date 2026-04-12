using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Utility;

namespace TechShop_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeedController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ApiResponse _response;

        public SeedController(ApplicationDbContext db)
        {
            _db = db;
            _response = new ApiResponse();
        }

        [HttpPost("seedDatabase")]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<ActionResult<ApiResponse>> SeedDatabase()
        {
            try
            {
                var products = await _db.Products.ToListAsync();
                if (!products.Any())
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Database must have products to seed orders.");
                    return BadRequest(_response);
                }

                // 1. CONFIGURATION
                var yearlyUserPlan = new Dictionary<int, int>
                {
                    { 2022, 640 },
                    { 2023, 320 },
                    { 2024, 400 },
                    { 2025, 450 }
                };

                double currentDailyBase = 2.0;       // Starting orders per day in Jan 2022
                double dailyGrowthFactor = 1.00072;   // ~30% volume growth per year

                var activeUsersPool = new List<ApplicationUser>();
                var batchOrders = new List<OrderHeader>();
                var batchReviews = new List<Comment>();

                foreach (var plan in yearlyUserPlan)
                {
                    int currentYear = plan.Key;
                    int newUsersCount = plan.Value;

                    var newUsers = GenerateUsersForYear(newUsersCount, currentYear);
                    _db.ApplicationUsers.AddRange(newUsers);
                    await _db.SaveChangesAsync();

                    activeUsersPool.AddRange(newUsers);

                    DateTime pointer = new DateTime(currentYear, 1, 1);
                    DateTime endOfYear = new DateTime(currentYear, 12, 31);

                    while (pointer <= endOfYear)
                    {
                        double seasonality = CalculateDailySeasonality(pointer);

                        // Add 20% random noise so the charts aren't perfectly smooth
                        double dailyVariance = 0.8 + (Random.Shared.NextDouble() * 0.4);
                        int ordersToday = (int)(currentDailyBase * seasonality * dailyVariance);

                        // Generate today's orders
                        for (int i = 0; i < ordersToday; i++)
                        {
                            var (order, reviews) = GenerateOrderWithReviews(pointer, activeUsersPool, products);
                            batchOrders.Add(order);
                            batchReviews.AddRange(reviews);
                        }

                        // Save in chunks to prevent RAM overflow
                        if (batchOrders.Count >= 500)
                        {
                            _db.OrderHeaders.AddRange(batchOrders);
                            _db.Comments.AddRange(batchReviews);
                            await _db.SaveChangesAsync();
                            batchOrders.Clear();
                            batchReviews.Clear();
                        }

                        // Advance day and grow the business
                        pointer = pointer.AddDays(1);
                        currentDailyBase *= dailyGrowthFactor;
                    }
                }

                if (batchOrders.Any())
                {
                    _db.OrderHeaders.AddRange(batchOrders);
                    await _db.SaveChangesAsync();
                }

                await recalculateRatingsForAllProducts();

                _response.StatusCode = System.Net.HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return StatusCode(500, _response);
            }
        }

        // --- HELPER METHODS ---

        private (OrderHeader order, List<Comment> reviews) GenerateOrderWithReviews(DateTime date, List<ApplicationUser> users, List<Product> products)
        {
            var user = users[Random.Shared.Next(0, users.Count)];
            DateTime orderDate = date.Date.AddHours(Random.Shared.Next(8, 22));

            var order = new OrderHeader
            {
                ApplicationUserId = user.Id,
                PickupEmail = user.Email,
                PickupName = user.Name,
                PickupPhoneNumber = user.PhoneNumber,
                OrderDate = orderDate,
                Status = SD.status_delivered,
                OrderDetails = new List<OrderDetail>()
            };

            var reviews = new List<Comment>();
            int productCount = Random.Shared.Next(1, 4);
            var selectedProducts = new HashSet<Product>();

            while (selectedProducts.Count < productCount) selectedProducts.Add(GetProductByPriceWeight(products));

            foreach (var prod in selectedProducts)
            {
                int qty = Random.Shared.Next(1, 100) > 90 ? 2 : 1;
                order.OrderDetails.Add(new OrderDetail { ProductId = prod.Id, ItemName = prod.Name, Price = prod.Price, Quantity = qty });
                order.OrderTotal += prod.Price * qty;
                order.TotalItems += qty;

                // 80% chance to leave a review for each item purchased
                if (Random.Shared.Next(1, 101) <= 80)
                {
                    reviews.Add(GenerateReview(user.Id, prod.Id, orderDate));
                }
            }

            return (order, reviews);
        }

        private Comment GenerateReview(string userId, int productId, DateTime orderDate)
        {
            // Sentiment: 70% chance of 4-5 stars, 20% 3 stars, 10% 1-2 stars
            int roll = Random.Shared.Next(1, 101);
            int rating = roll > 30 ? Random.Shared.Next(4, 6) : (roll > 10 ? 3 : Random.Shared.Next(1, 3));

            string[] positive = { "Amazing quality!", "Exceeded expectations.", "Works perfectly.", "Fast shipping and great item.", "Highly recommend." };
            string[] neutral = { "It's okay for the price.", "Does the job, but nothing special.", "Decent quality.", "Took a while to arrive." };
            string[] negative = { "Not what I expected.", "Broke after a week.", "Poor quality material.", "Wait for a sale, not worth full price." };

            string comment = rating >= 4 ? positive[Random.Shared.Next(positive.Length)] :
                             (rating == 3 ? neutral[Random.Shared.Next(neutral.Length)] : negative[Random.Shared.Next(negative.Length)]);

            return new Comment
            {
                ProductId = productId,
                ApplicationUserId = userId,
                Rating = rating,
                Content = comment,
                // Review is left 3 to 10 days AFTER the order
                CreatedAt = orderDate.AddDays(Random.Shared.Next(3, 11))
            };
        }

        private List<ApplicationUser> GenerateUsersForYear(int count, int year)
        {
            var users = new List<ApplicationUser>();
            for (int i = 0; i < count; i++)
            {
                string id = Guid.NewGuid().ToString();
                users.Add(new ApplicationUser
                {
                    Id = id,
                    UserName = $"user_{year}_{id.Substring(0, 5)}@test.com",
                    Email = $"user_{year}_{id.Substring(0, 5)}@test.com",
                    NormalizedEmail = $"USER_{year}_{id.Substring(0, 5)}@TEST.COM",
                    Name = $"Customer {id.Substring(0, 5)}",
                    PhoneNumber = "555-" + Random.Shared.Next(1000, 9999)
                    // Note: PasswordHash is left null. These are mock users for analytics only.
                });
            }
            return users;
        }

        private double CalculateDailySeasonality(DateTime pointer)
        {
            if (pointer.Month == 11)
            {
                // Nov: Ramps up from 1.0 to 2.0 towards Black Friday
                return 1.0 + (pointer.Day / 30.0);
            }
            if (pointer.Month == 12)
            {
                if (pointer.Day <= 20) return 2.2; // Peak Holidays

                // Dec 21 to Dec 31 tapers from 2.2 down to 0.8
                int daysPastPeak = pointer.Day - 20;
                return 2.2 - (daysPastPeak * (1.4 / 11.0));
            }
            if (pointer.Month == 1)
            {
                return 0.8; // Post-holiday slump (connects perfectly to Dec 31)
            }

            return 1.0; // Normal month
        }

        private Product GetProductByPriceWeight(List<Product> products)
        {
            // Inverse Price Weighting (1 / Sqrt(Price))
            var weightedList = products.Select(p => new {
                Item = p,
                Weight = 1.0 / Math.Sqrt((double)p.Price)
            }).ToList();

            double totalWeight = weightedList.Sum(x => x.Weight);
            double r = Random.Shared.NextDouble() * totalWeight;
            double cumulative = 0;

            foreach (var w in weightedList)
            {
                cumulative += w.Weight;
                if (r <= cumulative) return w.Item;
            }
            return products.First(); // Fallback
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
    }
}