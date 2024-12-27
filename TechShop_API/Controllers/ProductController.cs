using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Net;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Models.Dto;

namespace TechShop_API.Controllers
{
    [Route("api/Product")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private ApiResponse _response;
        public ProductController(ApplicationDbContext db)
        {
            _db = db;
            _response = new ApiResponse();
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            _response.Result = _db.Products.Include(u => u.Category)
                .Select(product => new
                {
                    product.Id,
                    product.CategoryId,
                    Category = new { product.Category.Name }, // Only select the name
                    product.Name,
                    product.Description,
                    product.Price,
                    product.Stock,
                    Images = product.ProductImages.Select(image => image.Url).ToList()
                });
            _response.StatusCode = System.Net.HttpStatusCode.OK;
            return Ok(_response);
        }

        // Get products by category ID
        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsByCategory(int categoryId)
        {
            IQueryable<Product> productsQuery = _db.Products;//.Include(p => p.ProductImages);

            if (categoryId != 0)
            {
                // Only filter by category if categoryId is not 0
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId);
            }

            var products = await productsQuery
                .Select(product => new
                {
                    product.Id,
                    product.CategoryId,
                    Category = new { product.Category.Name }, // Only select the name
                    product.Name,
                    product.Description,
                    product.Price,
                    product.Stock,
                    Images = product.ProductImages.Select(image => image.Url).ToList()
                })
                .ToListAsync();
            _response.Result = products;
            _response.StatusCode = System.Net.HttpStatusCode.OK;
            return Ok(_response);
        }


        [HttpPost]
        public async Task<ActionResult<ApiResponse>> CreateProduct([FromBody] ProductCreateDTO productCreateDTO)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var categoryId = productCreateDTO.CategoryId;
                    if (_db.Categories.FirstOrDefault(u => u.Id == categoryId) == null)
                    {
                        throw new ArgumentException("Category doesn't exist");
                    }
                    
                    Product ProductToCreate = new()
                    {
                        Name = productCreateDTO.Name,
                        Description = productCreateDTO.Description,
                        CategoryId = productCreateDTO.CategoryId,
                        Price = productCreateDTO.Price,
                        Stock = productCreateDTO.Stock,
                    };

                    _db.Products.Add(ProductToCreate);
                    _db.SaveChanges();
                    _response.Result = ProductToCreate;
                    _response.StatusCode = HttpStatusCode.Created;
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
        public async Task<ActionResult<ApiResponse>> DeleteProduct(int id)
        {
            try
            {
                if (id == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    return BadRequest();
                }

                Product productFromDb = await _db.Products.FindAsync(id);
                if (productFromDb == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    return BadRequest();
                }

                var productAttributes = _db.ProductAttributes.Where(pa => pa.ProductId == id);
                _db.ProductAttributes.RemoveRange(productAttributes);
                var productImages = _db.ProductImages.Where(pi => pi.ProductId == id);
                _db.ProductImages.RemoveRange(productImages);
                _db.Products.Remove(productFromDb);

                _db.SaveChanges();
                _response.StatusCode = HttpStatusCode.NoContent;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                    = new List<string>() { ex.ToString() };
                return BadRequest(_response);
            }

            return _response;
        }

    }
}
