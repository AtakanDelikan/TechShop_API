using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;
using TechShop_API.Controllers;
using TechShop_API.Models;
using TechShop_API.Services.Interfaces;
using Xunit;

namespace TechShop.Tests.Controllers
{
    public class PaymentControllerTests
    {
        private readonly Mock<IPaymentService> _mockPaymentService;
        private readonly PaymentController _controller;

        public PaymentControllerTests()
        {
            _mockPaymentService = new Mock<IPaymentService>();
            _controller = new PaymentController(_mockPaymentService.Object);
        }

        [Fact]
        public async Task MakePayment_ReturnsOk_WhenPaymentSuccessful()
        {
            // Arrange
            var userId = "user123";
            var mockResponse = new ApiResponse
            {
                IsSuccess = true,
                StatusCode = HttpStatusCode.OK,
                Result = new ShoppingCart
                {
                    UserId = userId,
                    CartTotal = 100,
                    StripePaymentIntentId = "pi_test",
                    ClientSecret = "secret_test"
                }
            };

            _mockPaymentService
                .Setup(s => s.MakePaymentAsync(userId))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _controller.MakePayment(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse>(okResult.Value);

            Assert.True(response.IsSuccess);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Result);
        }

        [Fact]
        public async Task MakePayment_ReturnsBadRequest_WhenPaymentFails()
        {
            // Arrange
            var userId = "user123";
            var mockResponse = new ApiResponse
            {
                IsSuccess = false,
                StatusCode = HttpStatusCode.BadRequest,
                ErrorMessages = new List<string> { "Invalid cart" }
            };

            _mockPaymentService
                .Setup(s => s.MakePaymentAsync(userId))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _controller.MakePayment(userId);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse>(badRequest.Value);

            Assert.False(response.IsSuccess);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("Invalid cart", response.ErrorMessages);
        }

        [Fact]
        public async Task MakePayment_ReturnsInternalServerError_OnUnexpectedException()
        {
            // Arrange
            var userId = "user123";
            _mockPaymentService
                .Setup(s => s.MakePaymentAsync(userId))
                .ThrowsAsync(new Exception("Something went wrong"));

            // Act
            var result = await _controller.MakePayment(userId);

            // Assert
            var objResult = Assert.IsType<ObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse>(objResult.Value);

            Assert.False(response.IsSuccess);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }
    }
}
