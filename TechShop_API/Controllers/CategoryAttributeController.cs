using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services.Interfaces;
using TechShop_API.Utility;

namespace TechShop_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryAttributeController : ControllerBase
    {
        private readonly ICategoryAttributeService _service;

        public CategoryAttributeController(ICategoryAttributeService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse>> GetAll()
        {
            var response = new ApiResponse
            {
                StatusCode = HttpStatusCode.OK,
                Result = await _service.GetAllAsync()
            };

            return Ok(response);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse>> Get(int id)
        {
            var details = await _service.GetCategoryAttributeDetailsAsync(id);

            if (details == null)
                return NotFound(new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.NotFound
                });

            return Ok(new ApiResponse
            {
                StatusCode = HttpStatusCode.OK,
                Result = details
            });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<ActionResult<ApiResponse>> Create(CategoryAttributeCreateDTO dto)
        {
            var response = await _service.CreateAsync(dto);
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<ActionResult<ApiResponse>> Update(int id, CategoryAttributeUpdateDTO dto)
        {
            var response = await _service.UpdateAsync(id, dto);
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<ActionResult<ApiResponse>> Delete(int id)
        {
            var response = await _service.DeleteAsync(id);
            return StatusCode((int)response.StatusCode, response);
        }
    }
}
