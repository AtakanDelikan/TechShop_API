using Microsoft.EntityFrameworkCore;
using Moq;
using System.Net;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Services;
using TechShop_API.Services.Interfaces;
using Xunit;

namespace TechShop.Tests.Services
{
    public class PaymentServiceTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IPaymentGateway> _mockGateway;
        private readonly PaymentService _service;

        public PaymentServiceTests()
        {
            // Configure in-memory EF Core DB
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "PaymentServiceTests")
                .Options;

            _context = new ApplicationDbContext(options);

            // Mock Stripe gateway
            _mockGateway = new Mock<IPaymentGateway>();
            _mockGateway.Setup(g => g.CreatePaymentIntentAsync(It.IsAny<decimal>(), It.IsAny<string>()))
                        .ReturnsAsync(("test_intent_id", "test_secret"));

            _service = new PaymentService(_context, _mockGateway.Object);
        }

        [Fact]
        public async Task MakePaymentAsync_ReturnsSuccess_WhenCartIsValid()
        {
            // Arrange
            var userId = "user123";

            var product = new Product { Id = 1, Name = "Laptop", Price = 1000 };
            var cart = new ShoppingCart
            {
                UserId = userId,
                CartItems = new List<CartItem>
                {
                    new CartItem { Product = product, Quantity = 2 }
                }
            };

            _context.Products.Add(product);
            _context.ShoppingCarts.Add(cart);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.MakePaymentAsync(userId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.NotNull(result.Result);
            Assert.Equal("test_intent_id", ((ShoppingCart)result.Result).StripePaymentIntentId);
        }

        [Fact]
        public async Task MakePaymentAsync_ReturnsBadRequest_WhenCartIsEmpty()
        {
            // Arrange
            var userId = "userEmpty";
            _context.ShoppingCarts.Add(new ShoppingCart { UserId = userId, CartItems = new List<CartItem>() });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.MakePaymentAsync(userId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }
    }
}
