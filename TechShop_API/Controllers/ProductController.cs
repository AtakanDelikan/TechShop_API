using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Drawing.Printing;
using System.Net;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Utility;

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

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            Product product = _db.Products
                .Include(p => p.ProductImages)
                .FirstOrDefault(u => u.Id == id);

            if (product == null)
            {
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.IsSuccess = false;
                return NotFound(_response);
            }

            var dbProductAttributes = _db.ProductAttributes
                .Where(pa => pa.ProductId == id)
                .Include(pa => pa.CategoryAttribute)
                .ToList();

            var productAttributes = dbProductAttributes.Select(pa => new ProductAttributeDTO
            {
                Id = pa.Id,
                CategoryAttributeId = pa.CategoryAttributeId,
                Name = pa.CategoryAttribute.AttributeName,
                Value = pa.CategoryAttribute.DataType switch
                {
                    SD.DataTypeEnum.String => pa.String,
                    SD.DataTypeEnum.Integer => pa.Integer?.ToString(),
                    SD.DataTypeEnum.Decimal => pa.Decimal?.ToString("F2"),
                    SD.DataTypeEnum.Boolean => pa.Boolean?.ToString().ToLower(),
                    _ => null // Handle unexpected enum values
                }
            }).ToList();
            product.ProductAttributes = productAttributes;

            if (product != null && product.ProductImages != null)
            {
                product.ProductImages = product.ProductImages.OrderBy(pi => pi.DisplayOrder).ToList();
            }

            _response.Result = product;
            _response.StatusCode = HttpStatusCode.OK;
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

        //TODO put this into separate file
        #region Filter Helper Functions
        private IQueryable<Product> filterProduct(IQueryable<Product> productsQuery, string attributeFilters)
        {
            string[] attributeFilterParts = attributeFilters.Split(';');
            foreach (string part in attributeFilterParts)
            {
                // Split by '[' to separate key and values
                int startBracketIndex = part.IndexOf('[');
                if (startBracketIndex == -1 || !part.EndsWith("]"))
                    continue; // Invalid format, skip

                // Extract key
                string keyPart = part.Substring(0, startBracketIndex);
                if (int.TryParse(keyPart, out int key))
                {
                    // Extract values and split by '﹐'
                    string valuesPart = part.Substring(startBracketIndex + 1, part.Length - startBracketIndex - 2);
                    string[] values = valuesPart.Split('﹐');

                    CategoryAttribute categoryAttribute = _db.CategoryAttributes.FirstOrDefault(ca => ca.Id == key);
                    if (categoryAttribute == null)
                    {
                        continue;
                    }

                    IQueryable<ProductAttribute> productAttributes = _db.ProductAttributes
                        .Where(pa => pa.CategoryAttributeId == key);
                    productAttributes = categoryAttribute.DataType switch
                    {
                        SD.DataTypeEnum.String => filterString(productAttributes, values),
                        SD.DataTypeEnum.Integer => filterInteger(productAttributes, values),
                        SD.DataTypeEnum.Decimal => filterDecimal(productAttributes, values),
                        SD.DataTypeEnum.Boolean => filterBoolean(productAttributes, values),
                        _ => productAttributes // Handle unexpected enum values
                    };

                    productsQuery = productsQuery
                        .Where(product => productAttributes.Any(pa => pa.ProductId == product.Id));
                }
            }
            return productsQuery;

        }

        private IQueryable<ProductAttribute> filterString(IQueryable<ProductAttribute> productAttributes, string[] values)
        {
            return productAttributes.Where(pa => values.Contains(pa.String));
        }

        private IQueryable<ProductAttribute> filterInteger(IQueryable<ProductAttribute> productAttributes, string[] values)
        {
            // make sure values has 2 integers
            if (values.Length != 2) { return productAttributes; }
            if (Int32.TryParse(values[0], out int min) && Int32.TryParse(values[1], out int max))
            {
            return productAttributes.Where(pa => pa.Integer.HasValue && pa.Integer>=min && pa.Integer<=max);
            } else { return productAttributes; }
        }

        private IQueryable<ProductAttribute> filterDecimal(IQueryable<ProductAttribute> productAttributes, string[] values)
        {
            // make sure values has 2 decimals
            if (values.Length != 2) { return productAttributes; }
            if (double.TryParse(values[0], out double min) && double.TryParse(values[1], out double max))
            {
                return productAttributes.Where(pa => pa.Decimal.HasValue && pa.Decimal >= min && pa.Decimal <= max);
            }
            else { return productAttributes; }
        }

        private IQueryable<ProductAttribute> filterBoolean(IQueryable<ProductAttribute> productAttributes, string[] values)
        {
            // make sure values has 1 boolean
            if (values.Length != 1) { return productAttributes; }
            if (values[0] == "true")
            {
                return productAttributes.Where(pa => pa.Boolean.HasValue && pa.Boolean == true);
            } else if (values[0] == "false")
            {
                return productAttributes.Where(pa => pa.Boolean.HasValue && pa.Boolean == false);
            }
            else { return productAttributes; }
        }

        #endregion

        [HttpGet("filter")]
        public async Task<ActionResult<ApiResponse>> GetFilteredProducts([FromQuery] Dictionary<string, string> filters, int pageNumber = 1, int pageSize = 10)
        {
            IQueryable<Product> productsQuery = _db.Products;

            if (filters.TryGetValue("category", out string categoryIdString))
            {
                if (Int32.TryParse(categoryIdString, out int categoryId))
                {
                    productsQuery = productsQuery.Where(p => p.CategoryId == categoryId);
                }
            }

            if (filters.TryGetValue("price", out string? priceString) && !string.IsNullOrWhiteSpace(priceString))
            {
                var values = priceString.Split('﹐');

                if (values.Length == 2 &&
                    decimal.TryParse(values[0], out decimal minPrice) &&
                    decimal.TryParse(values[1], out decimal maxPrice) &&
                    minPrice <= maxPrice)
                {
                    productsQuery = productsQuery.Where(p => p.Price >= minPrice && p.Price <= maxPrice);
                }
            }

            if (filters.TryGetValue("attributes", out string attributeFilters) && attributeFilters!=null)
            {
                productsQuery = filterProduct(productsQuery, attributeFilters);
            }

            if (filters.TryGetValue("search", out string? searchTerm) && !string.IsNullOrWhiteSpace(searchTerm))
            {
                var productIdsFromAttributes = _db.ProductAttributes
                .Where(pa => pa.String.Contains(searchTerm))
                .Select(pa => pa.ProductId);

                productsQuery = productsQuery
                    .Where(p => p.Name.Contains(searchTerm) ||
                                p.Description.Contains(searchTerm) ||
                                productIdsFromAttributes.Contains(p.Id));
            }

            int totalProducts = await productsQuery.CountAsync(); // Total matching products
            int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize); // Calculate pages

            var products = await productsQuery
                    .OrderBy(p => p.Name)
                    .Skip(pageSize * (pageNumber - 1))
                    .Take(pageSize)
                    .Include(p => p.ProductImages)
                    .ToListAsync();
            _response.Result = new
            {
                Products = products,
                TotalItems = totalProducts,
                TotalPages = totalPages,
                CurrentPage = pageNumber
            };
            _response.StatusCode = HttpStatusCode.OK;
            return _response;
        }

        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse>> GetSearchedProducts(string searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var productIdsFromAttributes = _db.ProductAttributes
                .Where(pa => pa.String.Contains(searchTerm))
                .Select(pa => pa.ProductId);

                // Query to get total count before pagination
                var query = _db.Products
                    .Where(p => p.Name.Contains(searchTerm) ||
                                p.Description.Contains(searchTerm) ||
                                productIdsFromAttributes.Contains(p.Id));

                int totalProducts = await query.CountAsync(); // Total matching products
                int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize); // Calculate pages

                var products = await query
                    .OrderBy(p => p.Name)
                    .Skip(pageSize * (pageNumber - 1))
                    .Take(pageSize)
                    .ToListAsync();
                _response.Result = new
                {
                    Products = products,
                    TotalItems = totalProducts,
                    TotalPages = totalPages,
                    CurrentPage = pageNumber
                };
                _response.StatusCode = HttpStatusCode.OK;
                return _response;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                    = new List<string>() { ex.ToString() };
            }
            return _response;
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

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse>> UpdateProduct(int id, [FromBody] ProductUpdateDTO ProductUpdateDTO)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (ProductUpdateDTO == null)
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

                    productFromDb.Name = ProductUpdateDTO.Name;
                    productFromDb.Description = ProductUpdateDTO.Description;
                    productFromDb.CategoryId = ProductUpdateDTO.CategoryId;
                    productFromDb.Price = ProductUpdateDTO.Price;
                    productFromDb.Stock = ProductUpdateDTO.Stock;


                    _db.Products.Update(productFromDb);
                    _db.SaveChanges();
                    _response.StatusCode = HttpStatusCode.NoContent;
                    return Ok(_response);
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
