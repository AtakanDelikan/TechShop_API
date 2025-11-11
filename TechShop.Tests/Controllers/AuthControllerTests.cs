using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using System.Threading.Tasks;
using TechShop_API.Controllers;
using TechShop_API.Models.Dto;
using TechShop_API.Services.Interfaces;
using Xunit;

namespace TechShop.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _controller = new AuthController(_mockAuthService.Object);
        }

        [Fact]
        public async Task Login_ReturnsOk_WhenCredentialsValid()
        {
            // Arrange
            var dto = new LoginRequestDTO { UserName = "user", Password = "pass" };
            var response = new LoginResponseDTO { Email = "user@example.com", Token = "abc123" };
            _mockAuthService.Setup(s => s.LoginAsync(dto)).ReturnsAsync(response);

            // Act
            var result = await _controller.Login(dto);
            var okResult = result as OkObjectResult;

            // Assert
            Assert.NotNull(okResult);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task Login_ReturnsBadRequest_WhenUnauthorized()
        {
            var dto = new LoginRequestDTO { UserName = "user", Password = "wrong" };
            _mockAuthService.Setup(s => s.LoginAsync(dto)).ThrowsAsync(new UnauthorizedAccessException("Invalid username or password"));

            var result = await _controller.Login(dto);
            var badRequest = result as BadRequestObjectResult;

            Assert.NotNull(badRequest);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task Register_ReturnsOk_WhenSuccessful()
        {
            var dto = new RegisterRequestDTO { UserName = "newuser", Password = "123", Role = "Customer" };
            _mockAuthService.Setup(s => s.RegisterAsync(dto)).ReturnsAsync(true);

            var result = await _controller.Register(dto);
            var okResult = result as OkObjectResult;

            Assert.NotNull(okResult);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task GetUserData_ReturnsOk_WhenAuthorized()
        {
            var mockUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user1")
            }));

            _controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = mockUser
            };

            var userDto = new UserDetailsDTO { Name = "Alice" };
            _mockAuthService.Setup(s => s.GetUserDataAsync("user1")).ReturnsAsync(userDto);

            var result = await _controller.GetUserData();
            var okResult = result.Result as OkObjectResult;

            Assert.NotNull(okResult);
            Assert.Equal(200, okResult.StatusCode);
        }
    }
}
