using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services.Interfaces;
using TechShop_API.Utility;

namespace TechShop_API.Controllers
{
    [Route("api/Category")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }


        // -----------------------
        // GET ALL + TREE
        // -----------------------
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var response = new ApiResponse();

            try
            {
                response.Result = await _categoryService.GetCategoriesTreeAsync();
                response.StatusCode = HttpStatusCode.OK;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessages.Add(ex.Message);
                response.StatusCode = HttpStatusCode.InternalServerError;
                return StatusCode(500, response);
            }
        }


        // -----------------------
        // GET BY ID
        // -----------------------
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var response = new ApiResponse();

            if (id <= 0)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add("Invalid ID");
                return BadRequest(response);
            }

            var category = await _categoryService.GetCategoryByIdAsync(id);

            if (category == null)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.NotFound;
                return NotFound(response);
            }

            response.Result = category;
            response.StatusCode = HttpStatusCode.OK;
            return Ok(response);
        }


        // -----------------------
        // SEARCH
        // -----------------------
        [HttpGet("search")]
        public async Task<IActionResult> SearchCategories(string searchTerm, int count = 5)
        {
            var response = new ApiResponse();

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add("searchTerm is required.");
                return BadRequest(response);
            }

            response.Result = await _categoryService.SearchCategoriesAsync(searchTerm, count);
            response.StatusCode = HttpStatusCode.OK;

            return Ok(response);
        }


        // -----------------------
        // CREATE
        // -----------------------
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateDTO dto)
        {
            var response = new ApiResponse();

            if (!ModelState.IsValid)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add("Invalid model state");
                return BadRequest(response);
            }

            try
            {
                var created = await _categoryService.CreateCategoryAsync(dto);

                response.Result = created;
                response.StatusCode = HttpStatusCode.Created;

                return StatusCode(201, response);
            }
            catch (ArgumentException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
                return StatusCode(500, response);
            }
        }


        // -----------------------
        // UPDATE
        // -----------------------
        [HttpPut("{id:int}")]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryUpdateDTO dto)
        {
            var response = new ApiResponse();

            if (!ModelState.IsValid)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                return BadRequest(response);
            }

            try
            {
                await _categoryService.UpdateCategoryAsync(id, dto);
                response.StatusCode = HttpStatusCode.NoContent;
                return StatusCode(204, response);
            }
            catch (KeyNotFoundException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.NotFound;
                response.ErrorMessages.Add(ex.Message);
                return NotFound(response);
            }
            catch (ArgumentException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
                return StatusCode(500, response);
            }
        }


        // -----------------------
        // DELETE
        // -----------------------
        [HttpDelete("{id:int}")]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var response = new ApiResponse();

            if (id <= 0)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                return BadRequest(response);
            }

            try
            {
                await _categoryService.DeleteCategoryAsync(id);
                response.StatusCode = HttpStatusCode.NoContent;
                return StatusCode(204, response);
            }
            catch (KeyNotFoundException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.NotFound;
                response.ErrorMessages.Add(ex.Message);
                return NotFound(response);
            }
            catch (InvalidOperationException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
                return StatusCode(500, response);
            }
        }
    }
}
