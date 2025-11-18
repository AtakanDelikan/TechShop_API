using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;
using TechShop_API.Controllers;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services.Interfaces;
using TechShop_API.Utility;
using Xunit;
using static TechShop_API.Utility.SD;

namespace TechShop.Tests.Services
{
    public class CategoryAttributeControllerTests
    {
        private readonly Mock<ICategoryAttributeService> _mockService;
        private readonly CategoryAttributeController _controller;

        public CategoryAttributeControllerTests()
        {
            _mockService = new Mock<ICategoryAttributeService>();
            _controller = new CategoryAttributeController(_mockService.Object);
        }

        [Fact]
        public async Task GetAll_ReturnsOk()
        {
            var list = new List<CategoryAttribute>
        {
            new CategoryAttribute { Id = 1, AttributeName = "Color" }
        };

            _mockService.Setup(s => s.GetAllAsync()).ReturnsAsync(list);

            var result = await _controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse>(ok.Value);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.IsSuccess);
            Assert.NotNull(response.Result);
        }

        [Fact]
        public async Task Get_ReturnsNotFound_WhenNull()
        {
            _mockService.Setup(s => s.GetCategoryAttributeDetailsAsync(5))
                .ReturnsAsync((object)null);

            var result = await _controller.Get(5);

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse>(notFound.Value);

            Assert.False(response.IsSuccess);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Create_ReturnsCreated()
        {
            var dto = new CategoryAttributeCreateDTO
            {
                CategoryId = 1,
                AttributeName = "Weight",
                DataType = DataTypeEnum.Decimal
            };

            var fakeResponse = new ApiResponse
            {
                StatusCode = HttpStatusCode.Created,
                Result = new CategoryAttribute()
            };

            _mockService.Setup(s => s.CreateAsync(dto)).ReturnsAsync(fakeResponse);

            var result = await _controller.Create(dto);

            var created = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(201, created.StatusCode);
        }

        [Fact]
        public async Task Update_ReturnsServiceStatus()
        {
            var dto = new CategoryAttributeUpdateDTO
            {
                AttributeName = "Size"
            };

            var serviceResponse = new ApiResponse
            {
                StatusCode = HttpStatusCode.NoContent
            };

            _mockService.Setup(s => s.UpdateAsync(1, dto))
                .ReturnsAsync(serviceResponse);

            var result = await _controller.Update(1, dto);

            var obj = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(204, obj.StatusCode);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent()
        {
            var serviceResponse = new ApiResponse
            {
                StatusCode = HttpStatusCode.NoContent
            };

            _mockService.Setup(s => s.DeleteAsync(8)).ReturnsAsync(serviceResponse);

            var result = await _controller.Delete(8);

            var obj = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(204, obj.StatusCode);
        }
    }
}