using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TechShop_API.Controllers;
using TechShop_API.Models;
using TechShop_API.Services.Interfaces;
using Xunit;

namespace TechShop.Tests.Controllers
{
    public class ProductControllerTests
    {
        private readonly Mock<IProductService> _mockService;
        private readonly ProductController _controller;

        public ProductControllerTests()
        {
            _mockService = new Mock<IProductService>();
            _controller = new ProductController(_mockService.Object);
        }

        [Fact]
        public async Task GetProducts_ReturnsOk()
        {
            var resp = new ApiResponse { StatusCode = HttpStatusCode.OK, Result = new object[] { } };
            _mockService.Setup(s => s.GetAllAsync()).ReturnsAsync(resp);

            var action = await _controller.GetProducts();
            var result = Assert.IsType<ObjectResult>(action);
            Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
        }

        [Fact]
        public async Task GetProduct_CallsServiceAndReturns()
        {
            var resp = new ApiResponse { StatusCode = HttpStatusCode.OK, Result = new object() };
            _mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(resp);

            var action = await _controller.GetProduct(1);
            var result = Assert.IsType<ObjectResult>(action);
            Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
        }

        [Fact]
        public async Task CreateProduct_UsesService()
        {
            var req = new TechShop_API.Models.Dto.ProductCreateDTO { CategoryId = 1, Name = "n", Description = "d", Price = 1, Stock = 1 };
            var resp = new ApiResponse { StatusCode = HttpStatusCode.Created, Result = new object() };
            _mockService.Setup(s => s.CreateAsync(req)).ReturnsAsync(resp);

            var action = await _controller.CreateProduct(req);
            var result = Assert.IsType<ObjectResult>(action);
            Assert.Equal((int)HttpStatusCode.Created, result.StatusCode);
        }

        [Fact]
        public async Task DeleteProduct_ReturnsStatusFromService()
        {
            _mockService.Setup(s => s.DeleteAsync(5)).ReturnsAsync(new ApiResponse { StatusCode = HttpStatusCode.NoContent });
            var action = await _controller.DeleteProduct(5);
            var result = Assert.IsType<ObjectResult>(action);
            Assert.Equal((int)HttpStatusCode.NoContent, result.StatusCode);
        }
    }
}
