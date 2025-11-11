using Microsoft.EntityFrameworkCore;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services;
using TechShop_API.Utility;
using Xunit;

namespace TechShop.Tests.Services
{
    public class OrderServiceTests
    {
        private readonly ApplicationDbContext _db;
        private readonly OrderService _service;

        public OrderServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _db = new ApplicationDbContext(options);
            _service = new OrderService(_db);
        }

        [Fact]
        public async Task CreateOrderAsync_CreatesOrderSuccessfully()
        {
            // Arrange
            var dto = new OrderHeaderCreateDTO
            {
                ApplicationUserId = "user1",
                PickupEmail = "user@example.com",
                PickupName = "User",
                PickupPhoneNumber = "123456789",
                OrderTotal = 100,
                TotalItems = 2,
                OrderDetailsDTO = new List<OrderDetailCreateDTO>
                {
                    new OrderDetailCreateDTO { ItemName = "Product A", ProductId = 1, Price = 50, Quantity = 1 },
                    new OrderDetailCreateDTO { ItemName = "Product B", ProductId = 2, Price = 50, Quantity = 1 }
                }
            };

            // Act
            var result = await _service.CreateOrderAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("user1", result.ApplicationUserId);
            Assert.Equal(SD.status_pending, result.Status);
            Assert.Equal(2, _db.OrderDetails.Count());
        }

        [Fact]
        public async Task GetAllOrdersAsync_ReturnsPagedResult()
        {
            // Arrange
            _db.OrderHeaders.AddRange(
                new OrderHeader { ApplicationUserId = "user1", OrderTotal = 10, OrderDate = DateTime.Now, PickupEmail = "user1", PickupName = "user1", PickupPhoneNumber = "123" },
                new OrderHeader { ApplicationUserId = "user2", OrderTotal = 20, OrderDate = DateTime.Now.AddMinutes(-1), PickupEmail = "user2", PickupName = "user2", PickupPhoneNumber = "123" }
            );
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.GetAllOrdersAsync(1, 10, "");

            // Assert
            Assert.NotNull(result);
            var props = result.GetType().GetProperties();
            Assert.Contains(props, p => p.Name == "OrderHeaders");
        }

        [Fact]
        public async Task GetOrdersByUserAsync_ReturnsUserOrders()
        {
            // Arrange
            _db.OrderHeaders.Add(new OrderHeader { ApplicationUserId = "user1", OrderTotal = 10, PickupEmail = "user1", PickupName = "user1", PickupPhoneNumber = "123" });
            _db.OrderHeaders.Add(new OrderHeader { ApplicationUserId = "user2", OrderTotal = 20, PickupEmail = "user2", PickupName = "user2", PickupPhoneNumber = "123" });
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.GetOrdersByUserAsync("user1");

            // Assert
            Assert.Single(result);
            Assert.Equal("user1", result.First().ApplicationUserId);
        }

        [Fact]
        public async Task UpdateOrderAsync_UpdatesExistingOrder()
        {
            // Arrange
            var order = new OrderHeader { OrderHeaderId = 1, PickupName = "Old Name", PickupEmail = "Old Email", PickupPhoneNumber = "Old Number" };
            _db.OrderHeaders.Add(order);
            await _db.SaveChangesAsync();

            var updateDto = new OrderHeaderUpdateDTO
            {
                OrderHeaderId = 1,
                PickupName = "New Name",
                PickupEmail = "New Email",
                PickupPhoneNumber = "New Number"
            };

            // Act
            var result = await _service.UpdateOrderAsync(1, updateDto);

            // Assert
            Assert.True(result);
            Assert.Equal("New Name", _db.OrderHeaders.First().PickupName);
            Assert.Equal("New Email", _db.OrderHeaders.First().PickupEmail);
            Assert.Equal("New Number", _db.OrderHeaders.First().PickupPhoneNumber);
        }
    }
}
