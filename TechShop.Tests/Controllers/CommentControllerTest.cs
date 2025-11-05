using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using TechShop_API.Controllers;
using TechShop_API.Models.Dto;
using TechShop_API.Services;
using TechShop_API.Services.Interfaces;
using Xunit;

namespace TechShop.Tests.Controllers
{
    public class CommentControllerTests
    {
        private readonly Mock<ICommentService> _mockService;
        private readonly CommentController _controller;

        public CommentControllerTests()
        {
            _mockService = new Mock<ICommentService>();
            _controller = new CommentController(_mockService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public async Task GetCommentsByProduct_ReturnsOkWithComments()
        {
            // Arrange
            var comments = new List<CommentDTO>
            {
                new CommentDTO { CommentId = 1, ProductId = 1, Content = "Nice!", Rating = 5, UserName = "Alice" }
            };

            _mockService.Setup(s => s.GetCommentsByProductAsync(1))
                        .ReturnsAsync(comments);

            // Act
            var actionResult = await _controller.GetCommentsByProduct(1) as OkObjectResult;

            // Assert
            Assert.NotNull(actionResult);
            Assert.Equal(200, actionResult.StatusCode);

            dynamic response = actionResult.Value;
            Assert.Single(response.Result);
            Assert.Equal("Nice!", response.Result[0].Content);
        }

        [Fact]
        public async Task CreateComment_ReturnsCreated_WhenUserAuthorized()
        {
            // Arrange
            var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user1")
            }));

            _controller.ControllerContext.HttpContext.User = userClaims;

            var dto = new CommentCreateDTO { ProductId = 1, Content = "Great!", Rating = 5 };

            _mockService.Setup(s => s.CreateCommentAsync(dto, userClaims))
                        .Returns(Task.CompletedTask);

            // Act
            var actionResult = await _controller.CreateComment(dto);
            var result = actionResult.Result as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(201, result.StatusCode); // Created
        }

        [Fact]
        public async Task CreateComment_ReturnsUnauthorized_WhenUserNotAuthorized()
        {
            // Arrange
            var dto = new CommentCreateDTO { ProductId = 1, Content = "Test", Rating = 5 };
            var userPrincipal = new ClaimsPrincipal(); // no claims

            _mockService.Setup(s => s.CreateCommentAsync(dto, userPrincipal))
                        .ThrowsAsync(new UnauthorizedAccessException("User not found"));

            _controller.ControllerContext.HttpContext.User = userPrincipal;

            // Act
            var actionResult = await _controller.CreateComment(dto);
            var result = actionResult.Result as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(401, result.StatusCode); // Unauthorized
        }

        [Fact]
        public async Task DeleteComment_ReturnsOk_WhenUserAuthorized()
        {
            // Arrange
            var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user1")
            }));

            _controller.ControllerContext.HttpContext.User = userClaims;

            _mockService.Setup(s => s.DeleteCommentAsync(10, userClaims))
                        .Returns(Task.CompletedTask);

            // Act
            var actionResult = await _controller.DeleteComment(10) as ObjectResult;

            // Assert
            Assert.NotNull(actionResult);
            Assert.Equal(200, actionResult.StatusCode);
        }

        [Fact]
        public async Task DeleteComment_ReturnsNotFound_WhenCommentDoesNotExist()
        {
            // Arrange
            var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user1")
            }));
            _controller.ControllerContext.HttpContext.User = userClaims;

            _mockService.Setup(s => s.DeleteCommentAsync(99, userClaims))
                        .ThrowsAsync(new KeyNotFoundException());

            // Act
            var result = await _controller.DeleteComment(99);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteComment_ReturnsForbid_WhenUserNotOwner()
        {
            // Arrange
            var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user2")
            }));
            _controller.ControllerContext.HttpContext.User = userClaims;

            _mockService.Setup(s => s.DeleteCommentAsync(10, userClaims))
                        .ThrowsAsync(new UnauthorizedAccessException());

            // Act
            var result = await _controller.DeleteComment(10);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }
    }
}
