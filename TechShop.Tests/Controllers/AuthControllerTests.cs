using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using TechShop_API.Controllers;
using TechShop_API.Models;
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

        // Helper to mock HttpContext with specific claims
        private void SetControllerContextUser(string claimType, string claimValue)
        {
            var mockUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(claimType, claimValue)
            }));

            _controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = mockUser
            };
        }

        private void SetEmptyControllerContextUser()
        {
            _controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            };
        }

        // --- LOGIN TESTS ---

        [Fact]
        public async Task Login_ReturnsOk_WhenCredentialsValid()
        {
            var dto = new LoginRequestDTO { UserName = "user", Password = "pass" };
            var response = new LoginResponseDTO { Email = "user@example.com", Token = "abc123" };
            _mockAuthService.Setup(s => s.LoginAsync(dto)).ReturnsAsync(response);

            var result = await _controller.Login(dto);
            var okResult = result as OkObjectResult;
            var apiResponse = okResult?.Value as ApiResponse;

            Assert.NotNull(okResult);
            Assert.Equal(200, okResult.StatusCode);
            Assert.True(apiResponse?.IsSuccess);
            Assert.Equal(response, apiResponse?.Result);
        }

        [Fact]
        public async Task Login_ReturnsBadRequest_WhenUnauthorized()
        {
            var dto = new LoginRequestDTO { UserName = "user", Password = "wrong" };
            _mockAuthService.Setup(s => s.LoginAsync(dto))
                .ThrowsAsync(new UnauthorizedAccessException("Invalid username or password"));

            var result = await _controller.Login(dto);
            var badRequest = result as BadRequestObjectResult;
            var apiResponse = badRequest?.Value as ApiResponse;

            Assert.NotNull(badRequest);
            Assert.Equal(400, badRequest.StatusCode);
            Assert.False(apiResponse?.IsSuccess);
            Assert.Contains("Invalid username or password", apiResponse?.ErrorMessages);
        }

        // --- REGISTER TESTS ---

        [Fact]
        public async Task Register_ReturnsOk_WhenSuccessful()
        {
            var dto = new RegisterRequestDTO { UserName = "newuser", Password = "123", Role = "Customer" };
            _mockAuthService.Setup(s => s.RegisterAsync(dto)).ReturnsAsync(true);

            var result = await _controller.Register(dto);
            var okResult = result as OkObjectResult;
            var apiResponse = okResult?.Value as ApiResponse;

            Assert.NotNull(okResult);
            Assert.Equal(200, okResult.StatusCode);
            Assert.True(apiResponse?.IsSuccess);
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenServiceReturnsFalse()
        {
            // Testing the path where Identity fails to create the user (e.g., bad password)
            var dto = new RegisterRequestDTO { UserName = "newuser", Password = "123" };
            _mockAuthService.Setup(s => s.RegisterAsync(dto)).ReturnsAsync(false);

            var result = await _controller.Register(dto);
            var badRequest = result as BadRequestObjectResult; // Because HttpStatusCode.BadRequest is assigned
            var apiResponse = badRequest?.Value as ApiResponse;

            Assert.NotNull(badRequest);
            Assert.Equal(400, badRequest.StatusCode);
            Assert.False(apiResponse?.IsSuccess);
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenUsernameExists()
        {
            // Testing the catch block for InvalidOperationException
            var dto = new RegisterRequestDTO { UserName = "existing" };
            _mockAuthService.Setup(s => s.RegisterAsync(dto))
                .ThrowsAsync(new InvalidOperationException("Username already exists"));

            var result = await _controller.Register(dto);
            var badRequest = result as BadRequestObjectResult;
            var apiResponse = badRequest?.Value as ApiResponse;

            Assert.NotNull(badRequest);
            Assert.Equal(400, badRequest.StatusCode);
            Assert.False(apiResponse?.IsSuccess);
            Assert.Contains("Username already exists", apiResponse?.ErrorMessages);
        }

        // --- REFRESH TESTS ---

        [Fact]
        public async Task Refresh_ReturnsOk_WhenSuccessful()
        {
            var dto = new TokenRequestDTO { AccessToken = "old", RefreshToken = "old" };
            var responseDto = new LoginResponseDTO { Token = "new", RefreshToken = "new" };

            _mockAuthService.Setup(s => s.RefreshAccessTokenAsync(dto)).ReturnsAsync(responseDto);

            var result = await _controller.Refresh(dto);
            var okResult = result as OkObjectResult;
            var apiResponse = okResult?.Value as ApiResponse;

            Assert.NotNull(okResult);
            Assert.Equal(200, okResult.StatusCode);
            Assert.True(apiResponse?.IsSuccess);
            Assert.Equal(responseDto, apiResponse?.Result);
        }

        [Fact]
        public async Task Refresh_ReturnsBadRequest_WhenExceptionThrown()
        {
            // Testing the catch(Exception ex) block
            var dto = new TokenRequestDTO { AccessToken = "bad", RefreshToken = "bad" };
            _mockAuthService.Setup(s => s.RefreshAccessTokenAsync(dto))
                .ThrowsAsync(new UnauthorizedAccessException("Invalid token"));

            var result = await _controller.Refresh(dto);
            var badRequest = result as BadRequestObjectResult;
            var apiResponse = badRequest?.Value as ApiResponse;

            Assert.NotNull(badRequest);
            Assert.Equal(400, badRequest.StatusCode);
            Assert.False(apiResponse?.IsSuccess);
            Assert.Contains("Invalid token", apiResponse?.ErrorMessages);
        }

        // --- LOGOUT TESTS ---

        [Fact]
        public async Task Logout_ReturnsBadRequest_WhenUserIdClaimIsMissing()
        {
            // Setup context with NO claims
            SetEmptyControllerContextUser();

            var result = await _controller.Logout();
            var badRequest = result as BadRequestResult;

            // Controller returns BadRequest() if string.IsNullOrEmpty(userId)
            Assert.NotNull(badRequest);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task Logout_ReturnsBadRequest_WhenRevokeFails()
        {
            // Controller uses "id" claim for Logout
            SetControllerContextUser("id", "user1");
            _mockAuthService.Setup(s => s.RevokeTokenAsync("user1")).ReturnsAsync(false);

            var result = await _controller.Logout();
            var badRequest = result as BadRequestResult;

            Assert.NotNull(badRequest);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task Logout_ReturnsOk_WhenSuccessful()
        {
            SetControllerContextUser("id", "user1");
            _mockAuthService.Setup(s => s.RevokeTokenAsync("user1")).ReturnsAsync(true);

            var result = await _controller.Logout();
            var okResult = result as OkObjectResult;
            var apiResponse = okResult?.Value as ApiResponse;

            Assert.NotNull(okResult);
            Assert.Equal(200, okResult.StatusCode);
            Assert.True(apiResponse?.IsSuccess);
        }

        // --- GET USER DATA TESTS ---

        [Fact]
        public async Task GetUserData_ReturnsOk_WhenAuthorized()
        {
            // Controller uses ClaimTypes.NameIdentifier for GetUserData
            SetControllerContextUser(ClaimTypes.NameIdentifier, "user1");
            var userDto = new UserDetailsDTO { Name = "Alice" };

            _mockAuthService.Setup(s => s.GetUserDataAsync("user1")).ReturnsAsync(userDto);

            var result = await _controller.GetUserData();
            var okResult = result.Result as OkObjectResult;
            var apiResponse = okResult?.Value as ApiResponse;

            Assert.NotNull(okResult);
            Assert.Equal(200, okResult.StatusCode);
            Assert.Equal(userDto, apiResponse?.Result);
        }

        [Fact]
        public async Task GetUserData_ReturnsUnauthorized_WhenServiceThrows()
        {
            SetControllerContextUser(ClaimTypes.NameIdentifier, "user1");

            _mockAuthService.Setup(s => s.GetUserDataAsync("user1"))
                .ThrowsAsync(new UnauthorizedAccessException("User not found"));

            var result = await _controller.GetUserData();
            var unauthorized = result.Result as UnauthorizedResult;

            Assert.NotNull(unauthorized);
            Assert.Equal(401, unauthorized.StatusCode);
        }

        // --- UPDATE USER DATA TESTS ---

        [Fact]
        public async Task UpdateUserDetails_ReturnsOk_WhenSuccessful()
        {
            // Controller uses ClaimTypes.NameIdentifier for UpdateUserDetails
            SetControllerContextUser(ClaimTypes.NameIdentifier, "user1");
            var dto = new UserDetailsDTO { Name = "New Name" };

            _mockAuthService.Setup(s => s.UpdateUserDataAsync("user1", dto)).ReturnsAsync(true);

            var result = await _controller.UpdateUserDetails(dto);
            var okResult = result.Result as OkObjectResult;
            var apiResponse = okResult?.Value as ApiResponse;

            Assert.NotNull(okResult);
            // Controller currently sets StatusCode = NoContent (204) but returns Ok() (200)
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task UpdateUserDetails_ReturnsUnauthorized_WhenServiceThrows()
        {
            SetControllerContextUser(ClaimTypes.NameIdentifier, "user1");
            var dto = new UserDetailsDTO { Name = "New Name" };

            _mockAuthService.Setup(s => s.UpdateUserDataAsync("user1", dto))
                .ThrowsAsync(new UnauthorizedAccessException());

            var result = await _controller.UpdateUserDetails(dto);
            var unauthorized = result.Result as UnauthorizedResult;

            Assert.NotNull(unauthorized);
            Assert.Equal(401, unauthorized.StatusCode);
        }
    }
}