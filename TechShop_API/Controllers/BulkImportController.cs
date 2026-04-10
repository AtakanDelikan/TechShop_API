using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services.Interfaces;
using TechShop_API.Utility;

namespace TechShop_API.Controllers
{
    [Route("api/BulkImport")]
    [ApiController]
    [Authorize(Roles = SD.Role_Admin)]
    public class BulkImportController : ControllerBase
    {
        private readonly IBulkImportService _bulkImportService;

        public BulkImportController(IBulkImportService bulkImportService)
        {
            _bulkImportService = bulkImportService;
        }

        [HttpPost("importCategories")]
        public async Task<ActionResult<ApiResponse>> ImportCategories(IFormFile file)
        {
            var response = await HandleImport(file, _bulkImportService.ImportCategoriesAsync);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        [HttpPost("importCategoryAttributes")]
        public async Task<ActionResult<ApiResponse>> ImportCategoryAttributes(IFormFile file)
        {
            var response = await HandleImport(file, _bulkImportService.ImportCategoryAttributesAsync);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        [HttpPost("importProducts")]
        public async Task<ActionResult<ApiResponse>> ImportProducts([FromForm] List<IFormFile> files)
        {
            var response = new ApiResponse { IsSuccess = true };
            foreach (var file in files)
            {
                var result = await HandleImport(file, _bulkImportService.ImportProductsAsync);

                if (!result.IsSuccess)
                {
                    return BadRequest(result); // Return the first error found
                }
            }
            return Ok(response);
        }

        private async Task<ApiResponse> HandleImport(IFormFile file, Func<Stream, Task<ApiResponse>> importFunc)
        {
            var response = new ApiResponse();
            if (file == null || file.Length == 0)
            {
                response.IsSuccess = false;
                response.ErrorMessages.Add("Invalid or empty file.");
                return response;
            }

            try
            {
                using var stream = file.OpenReadStream();
                response = await importFunc(stream);
                return response;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessages.Add($"Import failed: {ex.Message}");
                return response;
            }
        }
    }
}