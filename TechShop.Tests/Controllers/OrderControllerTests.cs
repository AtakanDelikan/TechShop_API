using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;
using TechShop_API.Controllers;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services.Interfaces;
using Xunit;

namespace TechShop.Tests.Controllers
{
    public class OrderControllerTests
    {
        private readonly Mock<IOrderService> _mockService;
        private readonly OrderController _controller;

        public OrderControllerTests()
        {
            _mockService = new Mock<IOrderService>();
            _controller = new OrderController(_mockService.Object);
        }

        [Fact]
        public async Task GetAllOrders_ReturnsOk_WhenServiceSucceeds()
        {
            // Arrange
            _mockService.Setup(s => s.GetAllOrdersAsync(1, 20, ""))
                        .ReturnsAsync(new { TotalOrders = 1 });

            // Act
            var result = await _controller.GetAllOrders();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse>(okResult.Value);
            Assert.True(response.IsSuccess);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetOrderById_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            _mockService.Setup(s => s.GetOrderByIdAsync(999)).ReturnsAsync((OrderHeader?)null);

            // Act
            var result = await _controller.GetOrderById(999);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse>(notFound.Value);
            Assert.False(response.IsSuccess);
        }

        [Fact]
        public async Task CreateOrder_ReturnsCreated_WhenSuccessful()
        {
            // Arrange
            var dto = new OrderHeaderCreateDTO();
            var fakeOrder = new OrderHeader { OrderHeaderId = 1 };
            _mockService.Setup(s => s.CreateOrderAsync(dto)).ReturnsAsync(fakeOrder);

            // Act
            var result = await _controller.CreateOrder(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse>(okResult.Value);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal(fakeOrder, response.Result);
        }

        [Fact]
        public async Task UpdateOrder_ReturnsOk_WhenUpdateSucceeds()
        {
            // Arrange
            var dto = new OrderHeaderUpdateDTO { OrderHeaderId = 1 };
            _mockService.Setup(s => s.UpdateOrderAsync(1, dto)).ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateOrder(1, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse>(okResult.Value);
            Assert.True(response.IsSuccess);
        }

        [Fact]
        public async Task UpdateOrder_ReturnsBadRequest_WhenServiceFails()
        {
            // Arrange
            var dto = new OrderHeaderUpdateDTO { OrderHeaderId = 1 };
            _mockService.Setup(s => s.UpdateOrderAsync(1, dto)).ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateOrder(1, dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse>(badRequest.Value);
            Assert.False(response.IsSuccess);
        }
    }
}
