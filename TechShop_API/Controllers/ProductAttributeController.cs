using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using TechShop_API.Models.Dto;
using TechShop_API.Services.Interfaces;
using TechShop_API.Utility;

namespace TechShop_API.Controllers
{
    [Route("api/ProductAttribute")]
    [ApiController]
    public class ProductAttributeController : ControllerBase
    {
        private readonly IProductAttributeService _service;

        public ProductAttributeController(IProductAttributeService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var resp = await _service.GetAllAsync();
            return StatusCode((int)resp.StatusCode, resp);
        }

        [HttpGet("byProduct/{productId:int}")]
        public async Task<IActionResult> GetByProduct(int productId)
        {
            var resp = await _service.GetByProductAsync(productId);
            return StatusCode((int)resp.StatusCode, resp);
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> Create([FromBody] ProductAttributeCreateDTO dto)
        {
            var resp = await _service.CreateAsync(dto);
            return StatusCode((int)resp.StatusCode, resp);
        }

        [HttpPut]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> Update([FromBody] ProductAttributeUpdateDTO dto)
        {
            var resp = await _service.UpdateAsync(dto);
            return StatusCode((int)resp.StatusCode, resp);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            var resp = await _service.DeleteAsync(id);
            return StatusCode((int)resp.StatusCode, resp);
        }
    }
}
