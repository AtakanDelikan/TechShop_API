using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using TechShop_API.Controllers;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services.Interfaces;
using TechShop_API.Utility;
using Xunit;

namespace TechShop.Tests.Controllers
{
    public class CategoryControllerTests
    {
        private readonly Mock<ICategoryService> _mockService;
        private readonly CategoryController _controller;

        public CategoryControllerTests()
        {
            _mockService = new Mock<ICategoryService>();
            _controller = new CategoryController(_mockService.Object);
        }

        [Fact]
        public async Task GetCategories_ReturnsOk_WithTree()
        {
            var tree = new List<CategoryDTO> { new CategoryDTO { Id = 1, Name = "A", SubCategories = new List<CategoryDTO>() } };
            _mockService.Setup(s => s.GetCategoriesTreeAsync()).ReturnsAsync(tree);

            var result = await _controller.GetCategories();
            var ok = Assert.IsType<OkObjectResult>(result);
            var resp = Assert.IsType<ApiResponse>(ok.Value);

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            Assert.Equal(tree, resp.Result);
        }

        [Fact]
        public async Task GetCategory_ReturnsBadRequest_ForInvalidId()
        {
            var result = await _controller.GetCategory(0);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var resp = Assert.IsType<ApiResponse>(bad.Value);

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
            Assert.False(resp.IsSuccess);
        }

        [Fact]
        public async Task GetCategory_ReturnsNotFound_WhenServiceReturnsNull()
        {
            _mockService.Setup(s => s.GetCategoryByIdAsync(5)).ReturnsAsync((CategoryDetailsDTO?)null);

            var result = await _controller.GetCategory(5);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var resp = Assert.IsType<ApiResponse>(notFound.Value);

            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        [Fact]
        public async Task GetCategory_ReturnsOk_WhenFound()
        {
            var dto = new CategoryDetailsDTO { Id = 7, Name = "C" };
            _mockService.Setup(s => s.GetCategoryByIdAsync(7)).ReturnsAsync(dto);

            var result = await _controller.GetCategory(7);
            var ok = Assert.IsType<OkObjectResult>(result);
            var resp = Assert.IsType<ApiResponse>(ok.Value);

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            Assert.Equal(dto, resp.Result);
        }

        [Fact]
        public async Task SearchCategories_ReturnsBadRequest_WhenSearchTermMissing()
        {
            var result = await _controller.SearchCategories("", 5);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var resp = Assert.IsType<ApiResponse>(bad.Value);

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task CreateCategory_ReturnsCreated_WhenSuccess()
        {
            var createDto = new CategoryCreateDTO { Name = "X" };
            var created = new CategoryDetailsDTO { Id = 10, Name = "X" };
            _mockService.Setup(s => s.CreateCategoryAsync(createDto)).ReturnsAsync(created);

            var result = await _controller.CreateCategory(createDto);
            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(201, obj.StatusCode);
            var resp = Assert.IsType<ApiResponse>(obj.Value);
            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
            Assert.Equal(created, resp.Result);
        }

        [Fact]
        public async Task CreateCategory_ReturnsBadRequest_WhenParentInvalid()
        {
            var createDto = new CategoryCreateDTO { Name = "X", ParentCategoryId = 999 };
            _mockService.Setup(s => s.CreateCategoryAsync(createDto)).ThrowsAsync(new ArgumentException("Parent category does not exist."));

            var result = await _controller.CreateCategory(createDto);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var resp = Assert.IsType<ApiResponse>(bad.Value);

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
            Assert.False(resp.IsSuccess);
            Assert.Contains("Parent category does not exist", resp.ErrorMessages[0]);
        }

        [Fact]
        public async Task UpdateCategory_ReturnsNoContent_OnSuccess()
        {
            var dto = new CategoryUpdateDTO { Name = "U", Description = "d", ParentCategoryId = null };
            _mockService.Setup(s => s.UpdateCategoryAsync(1, dto)).Returns(Task.CompletedTask);

            var result = await _controller.UpdateCategory(1, dto);
            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(204, obj.StatusCode);
            var resp = Assert.IsType<ApiResponse>(obj.Value);
            Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
        }

        [Fact]
        public async Task UpdateCategory_ReturnsNotFound_WhenServiceThrowsNotFound()
        {
            var dto = new CategoryUpdateDTO { Name = "U" };
            _mockService.Setup(s => s.UpdateCategoryAsync(99, dto)).ThrowsAsync(new KeyNotFoundException("Category not found."));

            var result = await _controller.UpdateCategory(99, dto);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var resp = Assert.IsType<ApiResponse>(notFound.Value);
            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        [Fact]
        public async Task UpdateCategory_ReturnsBadRequest_WhenServiceThrowsArgument()
        {
            var dto = new CategoryUpdateDTO { Name = "U", ParentCategoryId = 1 };
            _mockService.Setup(s => s.UpdateCategoryAsync(2, dto)).ThrowsAsync(new ArgumentException("Parent does not exist."));

            var result = await _controller.UpdateCategory(2, dto);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var resp = Assert.IsType<ApiResponse>(bad.Value);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task DeleteCategory_ReturnsNoContent_OnSuccess()
        {
            _mockService.Setup(s => s.DeleteCategoryAsync(5)).Returns(Task.CompletedTask);

            var result = await _controller.DeleteCategory(5);
            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(204, obj.StatusCode);
            var resp = Assert.IsType<ApiResponse>(obj.Value);
            Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
        }

        [Fact]
        public async Task DeleteCategory_ReturnsNotFound_WhenServiceThrowsNotFound()
        {
            _mockService.Setup(s => s.DeleteCategoryAsync(99)).ThrowsAsync(new KeyNotFoundException("Category not found."));

            var result = await _controller.DeleteCategory(99);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var resp = Assert.IsType<ApiResponse>(notFound.Value);
            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        [Fact]
        public async Task DeleteCategory_ReturnsBadRequest_WhenServiceThrowsInvalidOp()
        {
            _mockService.Setup(s => s.DeleteCategoryAsync(7)).ThrowsAsync(new InvalidOperationException("Category has products."));

            var result = await _controller.DeleteCategory(7);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var resp = Assert.IsType<ApiResponse>(bad.Value);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
            Assert.Contains("Category has products", resp.ErrorMessages[0]);
        }
    }
}
