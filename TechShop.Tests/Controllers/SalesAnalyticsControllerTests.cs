using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;
using TechShop_API.Controllers;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Utility;
using Xunit;

namespace TechShop.Tests.Controllers
{
    public class SalesAnalyticsControllerTests
    {
        // Helper method to create a fresh, isolated in-memory database for each test
        private ApplicationDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task SalesAnalytics_ReturnsOk_WithCorrectAggregations()
        {
            var db = GetDbContext();
            var controller = new SalesAnalyticsController(db);

            var category = new Category { Id = 1, Name = "Laptops" };
            var product = new Product { Id = 1, Name = "Gaming Laptop", Price = 1000, CategoryId = 1, Category = category };

            db.OrderHeaders.Add(new OrderHeader
            {
                OrderHeaderId = 1,
                ApplicationUserId = "user1",
                Status = SD.status_delivered,
                OrderDate = new DateTime(2023, 1, 1),
                OrderTotal = 1000,
                TotalItems = 1,
                PickupName = "",
                PickupPhoneNumber = "",
                PickupEmail = "",
                OrderDetails = new List<OrderDetail>
                {
                    new OrderDetail { ProductId = 1, Product = product, ItemName = "Gaming Laptop", Price = 1000, Quantity = 1 }
                }
            });
            await db.SaveChangesAsync();

            var result = await controller.SalesAnalytics();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse>(okResult.Value);

            Assert.True(apiResponse.IsSuccess);
            Assert.NotNull(apiResponse.Result);

            var json = JsonSerializer.Serialize(apiResponse.Result);
            var parsedResult = JsonNode.Parse(json);

            Assert.Equal(1000, parsedResult["totalRevenue"]["2023"].GetValue<decimal>());
            Assert.Equal(1, parsedResult["totalOrders"]["2023"].GetValue<int>());
            Assert.Equal(1, parsedResult["uniqueCustomers"]["2023"].GetValue<int>());
            Assert.Equal(1, parsedResult["topSellingCategories"]["Laptops"].GetValue<int>());
        }

        [Fact]
        public async Task SalesAnalytics_ExcludesNonDeliveredOrders()
        {
            var db = GetDbContext();
            var controller = new SalesAnalyticsController(db);

            db.OrderHeaders.AddRange(new List<OrderHeader>
            {
                new OrderHeader { OrderHeaderId = 1, Status = SD.status_delivered, OrderDate = new DateTime(2023, 5, 5),
                    OrderTotal = 500, TotalItems = 1, ApplicationUserId = "u1", PickupName = "", PickupPhoneNumber = "", PickupEmail = ""  },
                new OrderHeader { OrderHeaderId = 2, Status = SD.status_pending, OrderDate = new DateTime(2023, 5, 6),
                    OrderTotal = 1000, TotalItems = 2, ApplicationUserId = "u2", PickupName = "", PickupPhoneNumber = "",PickupEmail = ""  },
                new OrderHeader { OrderHeaderId = 3, Status = SD.status_cancelled, OrderDate = new DateTime(2023, 5, 7),
                    OrderTotal = 200, TotalItems = 1, ApplicationUserId = "u3", PickupName = "", PickupPhoneNumber = "", PickupEmail = "" }
            });
            await db.SaveChangesAsync();

            var result = await controller.SalesAnalytics();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse>(okResult.Value);

            var json = JsonSerializer.Serialize(apiResponse.Result);
            var parsedResult = JsonNode.Parse(json);

            // Only the $500 delivered order should be counted
            Assert.Equal(500, parsedResult["totalRevenue"]["2023"].GetValue<decimal>());
            Assert.Equal(1, parsedResult["totalOrders"]["2023"].GetValue<int>());
        }

        [Fact]
        public async Task SalesAnalytics_HandlesLeapYearsCorrectly()
        {
            var db = GetDbContext();
            var controller = new SalesAnalyticsController(db);

            // Seed Data: 2023 (Non-Leap) and 2024 (Leap)
            db.OrderHeaders.AddRange(new List<OrderHeader>
            {
                new OrderHeader { OrderHeaderId = 1, Status = SD.status_delivered, OrderDate = new DateTime(2023, 12, 31),
                    OrderTotal = 100, PickupName = "", PickupPhoneNumber = "", PickupEmail = ""  },
                new OrderHeader { OrderHeaderId = 2, Status = SD.status_delivered, OrderDate = new DateTime(2024, 12, 31),
                    OrderTotal = 200, PickupName = "", PickupPhoneNumber = "", PickupEmail = ""  } // 2024 is a Leap Year
            });
            await db.SaveChangesAsync();

            // Act
            var result = await controller.SalesAnalytics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse>(okResult.Value);

            var json = JsonSerializer.Serialize(apiResponse.Result);
            var parsedResult = JsonNode.Parse(json);

            var revenueOverTime = parsedResult["revenueOverTime"];

            // 2023 should have 365 days
            Assert.Equal(365, revenueOverTime["2023"].AsArray().Count);
            // 2024 should have 366 days
            Assert.Equal(366, revenueOverTime["2024"].AsArray().Count);
        }

        [Fact]
        public async Task SalesAnalytics_ReturnsEmptyCollections_WhenNoDataExists()
        {
            var db = GetDbContext(); // Empty DB
            var controller = new SalesAnalyticsController(db);

            var result = await controller.SalesAnalytics();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse>(okResult.Value);

            Assert.True(apiResponse.IsSuccess);

            var json = JsonSerializer.Serialize(apiResponse.Result);
            var parsedResult = JsonNode.Parse(json);

            // Should return empty objects/arrays instead of null
            Assert.Empty(parsedResult["totalRevenue"].AsObject());
            Assert.Empty(parsedResult["topSellingCategories"].AsObject());
            Assert.Empty(parsedResult["revenueOverTime"].AsObject());
        }

        [Fact]
        public async Task SalesAnalytics_ReturnsBadRequest_OnDatabaseException()
        {
            var db = GetDbContext();
            var controller = new SalesAnalyticsController(db);

            // Dispose the context prematurely to force an ObjectDisposedException
            db.Dispose();

            var result = await controller.SalesAnalytics();

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse>(badRequestResult.Value);

            Assert.False(apiResponse.IsSuccess);
            Assert.NotEmpty(apiResponse.ErrorMessages);
            Assert.Contains("Error generating analytics", apiResponse.ErrorMessages[0]);
            Assert.Contains("disposed", apiResponse.ErrorMessages[0].ToLower()); // Ensure the specific exception message bubbled up
        }
    }
}