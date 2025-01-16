using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services;

namespace TechShop_API.Controllers
{
    [Route("api/Category")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private ApiResponse _response;
        private readonly CategoryService _categoryService;
        public CategoryController(ApplicationDbContext db, CategoryService categoryService)
        {
            _db = db;
            _response = new ApiResponse();
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            _response.Result = await _categoryService.GetCategoriesTreeAsync();
            _response.StatusCode = System.Net.HttpStatusCode.OK;
            return Ok(_response);
        }

        //TODO HttpGet API that returns the tree given a node id

        [HttpGet("{id:int}", Name = "GetCategory")]
        public async Task<IActionResult> GetCategory(int id)
        {
            if (id == 0)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                return BadRequest(_response);
            }
            Category category = _db.Categories
                .Include(c => c.ParentCategory)
                .FirstOrDefault(u => u.Id == id);
            if (category == null)
            {
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.IsSuccess = false;
                return NotFound(_response);
            }
            _response.Result = category;
            //_response.Result = null;
            //_response.Result = category.ParentCategory;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse>> CreateCategory([FromBody] CategoryCreateDTO categoryCreateDTO)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var parentCategoryId = categoryCreateDTO.ParentCategoryId;
                    if (parentCategoryId == 0)
                    {
                        parentCategoryId = null;
                    }

                    if (parentCategoryId != null &&
                        _db.Categories
                        .FirstOrDefault(u => u.Id == parentCategoryId) == null)
                    {
                        // if a parent Id is given but that parent doesn't exist in the database
                        throw new ArgumentException("Parent category doesn't exist");
                    }

                    Category CategoryToCreate = new()
                    {
                        Name = categoryCreateDTO.Name,
                        Description = categoryCreateDTO.Description,
                        ParentCategoryId = parentCategoryId,
                    };
                    _db.Categories.Add(CategoryToCreate);
                    _db.SaveChanges();
                    _response.Result = categoryCreateDTO;
                    _response.StatusCode = HttpStatusCode.Created;
                    _response.IsSuccess = true;
                    return _response;
                }
                else
                {
                    _response.IsSuccess = false;
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                    = new List<string>() { ex.ToString() };
            }

            return _response;
        }

        //TODO HttpPut API that updates category

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse>> DeleteCategory(int id)
        {
            try
            {
                if (id == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    return BadRequest();
                }

                Category categoryFromDb = await _db.Categories.FindAsync(id);
                if (categoryFromDb == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    return BadRequest();
                }
                // cannot delete if products exist of this category
                bool hasProduct = _db.Products.Any(u => u.CategoryId == id);
                if (hasProduct)
                {
                    throw new ArgumentException("Cannot delete category that has products");
                }
                // cannot delete if sub-category exist
                bool hasSubCategory = _db.Categories.Any(u => u.ParentCategoryId == id);
                if (hasSubCategory)
                {
                    throw new ArgumentException("Cannot delete category that has sub-category");
                }

                var categoryAttributes = _db.CategoryAttributes.Where(ca => ca.CategoryId == id);
                _db.CategoryAttributes.RemoveRange(categoryAttributes);
                _db.Categories.Remove(categoryFromDb);

                _db.SaveChanges();
                _response.StatusCode = HttpStatusCode.NoContent;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                    = new List<string>() { ex.ToString() };
            }

            return _response;
        }
        
    }
}
