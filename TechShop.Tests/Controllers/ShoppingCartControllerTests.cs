using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TechShop_API.Controllers;
using TechShop_API.Models;
using TechShop_API.Services.Interfaces;
using Xunit;

namespace TechShop.Tests.Controllers
{
    public class ShoppingCartControllerTests
    {
        private readonly Mock<IShoppingCartService> _mockService;
        private readonly ShoppingCartController _controller;

        public ShoppingCartControllerTests()
        {
            _mockService = new Mock<IShoppingCartService>();
            _controller = new ShoppingCartController(_mockService.Object);
        }

        [Fact]
        public async Task GetShoppingCart_ReturnsOk_WithCart()
        {
            var mockCart = new ShoppingCart();
            _mockService.Setup(s => s.GetShoppingCartAsync("user1"))
                        .ReturnsAsync(mockCart);

            var result = await _controller.GetShoppingCart("user1");
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse>(okResult.Value);

            Assert.True(response.IsSuccess);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(mockCart, response.Result);
        }

        [Fact]
        public async Task AddOrUpdateItemInCart_CallsService()
        {
            await _controller.AddOrUpdateItemInCart("user1", 1, 2);
            _mockService.Verify(s => s.AddOrUpdateItemAsync("user1", 1, 2), Times.Once);
        }

        [Fact]
        public async Task DeleteShoppingCart_ReturnsNoContent_WhenSuccessful()
        {
            _mockService.Setup(s => s.DeleteShoppingCartAsync(1)).Returns(Task.CompletedTask);

            var result = await _controller.DeleteShoppingCart(1);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse>(okResult.Value);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
