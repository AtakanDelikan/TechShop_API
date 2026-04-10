using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Text;
using TechShop_API.Controllers;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services.Interfaces;
using Xunit;

namespace TechShop_API.Tests.Controllers
{
    public class BulkImportControllerTests
    {
        private readonly Mock<IBulkImportService> _mockService;
        private readonly BulkImportController _controller;

        public BulkImportControllerTests()
        {
            _mockService = new Mock<IBulkImportService>();
            _controller = new BulkImportController(_mockService.Object);
        }

        // --- HELPER TO FAKE UPLOADED FILES ---
        private IFormFile CreateMockFile(string content, string fileName = "test.csv")
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var mockFile = new Mock<IFormFile>();

            mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
            mockFile.Setup(f => f.Length).Returns(stream.Length);
            mockFile.Setup(f => f.FileName).Returns(fileName);

            return mockFile.Object;
        }

        // ==========================================
        // SINGLE FILE TESTS (ImportCategories)
        // ==========================================

        [Fact]
        public async Task ImportCategories_ValidFile_ReturnsOk()
        {
            var file = CreateMockFile("Name,Description\nTest,Test Desc");
            var successResponse = new ApiResponse { IsSuccess = true };

            _mockService.Setup(s => s.ImportCategoriesAsync(It.IsAny<Stream>()))
                        .ReturnsAsync(successResponse);

            var result = await _controller.ImportCategories(file);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse>(okResult.Value);
            Assert.True(response.IsSuccess);
        }

        [Fact]
        public async Task ImportCategories_NullFile_ReturnsBadRequest()
        {
            var result = await _controller.ImportCategories(null);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse>(badRequestResult.Value);

            Assert.False(response.IsSuccess);
            Assert.Contains("Invalid or empty file", response.ErrorMessages[0]);

            // Verify service was NEVER called
            _mockService.Verify(s => s.ImportCategoriesAsync(It.IsAny<Stream>()), Times.Never);
        }

        [Fact]
        public async Task ImportCategories_EmptyFile_ReturnsBadRequest()
        {
            var emptyFile = CreateMockFile("");
            var mockEmptyFile = Mock.Get(emptyFile);
            mockEmptyFile.Setup(f => f.Length).Returns(0);

            var result = await _controller.ImportCategories(emptyFile);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse>(badRequestResult.Value);

            Assert.False(response.IsSuccess);
            Assert.Contains("Invalid or empty file", response.ErrorMessages[0]);
        }

        [Fact]
        public async Task ImportCategories_ServiceThrowsException_CatchesAndReturnsBadRequest()
        {
            var file = CreateMockFile("dummy data");

            // Simulate database timeout or file reading error inside the service
            _mockService.Setup(s => s.ImportCategoriesAsync(It.IsAny<Stream>()))
                        .ThrowsAsync(new Exception("Database connection lost"));

            var result = await _controller.ImportCategories(file);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse>(badRequestResult.Value);

            Assert.False(response.IsSuccess);
            Assert.Contains("Import failed: Database connection lost", response.ErrorMessages[0]);
        }

        // ==========================================
        // MULTIPLE FILES TESTS (ImportProducts)
        // ==========================================

        [Fact]
        public async Task ImportProducts_EmptyFileList_ReturnsOkWithNoAction()
        {
            var files = new List<IFormFile>();

            var result = await _controller.ImportProducts(files);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse>(okResult.Value);

            Assert.True(response.IsSuccess);
            _mockService.Verify(s => s.ImportProductsAsync(It.IsAny<Stream>()), Times.Never);
        }

        [Fact]
        public async Task ImportProducts_AllFilesValid_ReturnsOk()
        {
            var files = new List<IFormFile>
            {
                CreateMockFile("file1"),
                CreateMockFile("file2")
            };

            _mockService.Setup(s => s.ImportProductsAsync(It.IsAny<Stream>()))
                        .ReturnsAsync(new ApiResponse { IsSuccess = true });

            var result = await _controller.ImportProducts(files);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse>(okResult.Value);

            Assert.True(response.IsSuccess);

            // Verify the service was called exactly twice (once for each file)
            _mockService.Verify(s => s.ImportProductsAsync(It.IsAny<Stream>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ImportProducts_SecondFileFails_AbortsAndReturnsBadRequest()
        {
            var validFile = CreateMockFile("valid data");

            var emptyFile = CreateMockFile(""); // Invalid file
            var mockEmptyFile = Mock.Get(emptyFile);
            mockEmptyFile.Setup(f => f.Length).Returns(0);

            var files = new List<IFormFile> { validFile, emptyFile };

            // Setup service to return success ONLY for the valid file
            _mockService.Setup(s => s.ImportProductsAsync(It.IsAny<Stream>()))
                        .ReturnsAsync(new ApiResponse { IsSuccess = true });

            var result = await _controller.ImportProducts(files);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse>(badRequestResult.Value);

            Assert.False(response.IsSuccess);
            Assert.Contains("Invalid or empty file", response.ErrorMessages[0]);

            // Verify the service was only called ONCE. 
            // It should fail fast on the empty file without attempting to process it.
            _mockService.Verify(s => s.ImportProductsAsync(It.IsAny<Stream>()), Times.Once);
        }
    }
}