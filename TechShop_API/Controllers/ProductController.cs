using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services.Interfaces;
using TechShop_API.Utility;

namespace TechShop_API.Controllers
{
    [Route("api/Product")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _service;
        public ProductController(IProductService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var resp = await _service.GetAllAsync();
            return StatusCode((int)resp.StatusCode, resp);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var resp = await _service.GetByIdAsync(id);
            return StatusCode((int)resp.StatusCode, resp);
        }

        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetByCategory(int categoryId, int pageNumber = 1, int pageSize = 10)
        {
            var resp = await _service.GetByCategoryAsync(categoryId, pageNumber, pageSize);
            return StatusCode((int)resp.StatusCode, resp);
        }

        [HttpGet("filter")]
        public async Task<IActionResult> GetFiltered([FromQuery] Dictionary<string, string> filters, int pageNumber = 1, int pageSize = 10)
        {
            var resp = await _service.FilterProductsAsync(filters, pageNumber, pageSize);
            return StatusCode((int)resp.StatusCode, resp);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(string searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            var resp = await _service.SearchProductsAsync(searchTerm, pageNumber, pageSize);
            return StatusCode((int)resp.StatusCode, resp);
        }

        [Authorize(Roles = SD.Role_Admin)]
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] ProductCreateDTO dto)
        {
            var resp = await _service.CreateAsync(dto);
            return StatusCode((int)resp.StatusCode, resp);
        }

        [Authorize(Roles = SD.Role_Admin)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductUpdateDTO dto)
        {
            var resp = await _service.UpdateAsync(id, dto);
            return StatusCode((int)resp.StatusCode, resp);
        }

        [Authorize(Roles = SD.Role_Admin)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var resp = await _service.DeleteAsync(id);
            return StatusCode((int)resp.StatusCode, resp);
        }
    }
}
