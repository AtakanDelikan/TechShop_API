using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TechShop_API.Controllers;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services.Interfaces;
using Xunit;

namespace TechShop.Tests.Controllers
{
    public class ProductAttributeControllerTests
    {
        private readonly Mock<IProductAttributeService> _mockService;
        private readonly ProductAttributeController _controller;

        public ProductAttributeControllerTests()
        {
            _mockService = new Mock<IProductAttributeService>();
            _controller = new ProductAttributeController(_mockService.Object);
        }

        [Fact]
        public async Task GetAll_ReturnsOk()
        {
            var r = new ApiResponse { StatusCode = HttpStatusCode.OK, Result = new object[] { } };
            _mockService.Setup(s => s.GetAllAsync()).ReturnsAsync(r);

            var action = await _controller.GetAll();
            var result = Assert.IsType<ObjectResult>(action);
            Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
        }

        [Fact]
        public async Task Create_ReturnsCreated()
        {
            var dto = new ProductAttributeCreateDTO { ProductId = 1, CategoryAttributeId = 1, Value = "v" };
            var r = new ApiResponse { StatusCode = HttpStatusCode.Created, Result = new object() };
            _mockService.Setup(s => s.CreateAsync(dto)).ReturnsAsync(r);

            var action = await _controller.Create(dto);
            var result = Assert.IsType<ObjectResult>(action);
            Assert.Equal((int)HttpStatusCode.Created, result.StatusCode);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent()
        {
            _mockService.Setup(s => s.DeleteAsync(1)).ReturnsAsync(new ApiResponse { StatusCode = HttpStatusCode.NoContent });
            var action = await _controller.Delete(1);
            var result = Assert.IsType<ObjectResult>(action);
            Assert.Equal((int)HttpStatusCode.NoContent, result.StatusCode);
        }
    }
}
