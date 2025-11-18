using Microsoft.EntityFrameworkCore;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services.Interfaces;
using TechShop_API.Utility;

namespace TechShop_API.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _db;
        private readonly IProductFilterService _filter;

        public ProductService(ApplicationDbContext db, IProductFilterService filter)
        {
            _db = db;
            _filter = filter;
        }

        public async Task<ApiResponse> GetAllAsync()
        {
            var response = new ApiResponse();
            var products = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .AsNoTracking()
                .ToListAsync();

            var list = products.Select(ProductMapping.ToListItem).ToList();
            response.Result = list;
            response.StatusCode = System.Net.HttpStatusCode.OK;
            return response;
        }

        public async Task<ApiResponse> GetByIdAsync(int id)
        {
            var response = new ApiResponse();
            var product = await _db.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                response.IsSuccess = false;
                response.StatusCode = System.Net.HttpStatusCode.NotFound;
                response.ErrorMessages.Add("Product not found");
                return response;
            }

            var pa = await _db.ProductAttributes
                .Where(x => x.ProductId == id)
                .Include(x => x.CategoryAttribute)
                .ToListAsync();

            var attributeDtos = pa.Select(ProductMapping.ToAttributeDto).ToList();
            response.Result = ProductMapping.ToDetails(product, attributeDtos);
            response.StatusCode = System.Net.HttpStatusCode.OK;
            return response;
        }

        public async Task<ApiResponse> GetByCategoryAsync(int categoryId, int pageNumber, int pageSize)
        {
            var response = new ApiResponse();
            IQueryable<Product> query = _db.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Category)
                .AsQueryable();

            if (categoryId != 0)
                query = query.Where(p => p.CategoryId == categoryId);

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.Name)
                .Skip(pageSize * (pageNumber - 1))
                .Take(pageSize)
                .ToListAsync();

            var dtoList = items.Select(ProductMapping.ToListItem).ToList();

            response.Result = new
            {
                Products = dtoList,
                TotalItems = total,
                TotalPages = (int)Math.Ceiling((double)total / pageSize),
                CurrentPage = pageNumber
            };
            response.StatusCode = System.Net.HttpStatusCode.OK;
            return response;
        }

        public async Task<ApiResponse> FilterProductsAsync(Dictionary<string, string> filters, int pageNumber, int pageSize)
        {
            var response = new ApiResponse();
            IQueryable<Product> query = _db.Products.AsQueryable();

            query = _filter.ApplyFilters(query, filters);

            var total = await query.CountAsync();

            var items = await query
                .Include(p => p.ProductImages)
                .Include(p => p.Category)
                .OrderBy(p => p.Name)
                .Skip(pageSize * (pageNumber - 1))
                .Take(pageSize)
                .ToListAsync();

            var dtoList = items.Select(ProductMapping.ToListItem).ToList();

            response.Result = new
            {
                Products = dtoList,
                TotalItems = total,
                TotalPages = (int)Math.Ceiling((double)total / pageSize),
                CurrentPage = pageNumber
            };
            response.StatusCode = System.Net.HttpStatusCode.OK;
            return response;
        }

        public async Task<ApiResponse> SearchProductsAsync(string searchTerm, int pageNumber, int pageSize)
        {
            var response = new ApiResponse();
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                response.IsSuccess = false;
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                response.ErrorMessages.Add("searchTerm is required");
                return response;
            }

            IQueryable<Product> query = _db.Products;

            var productIdsFromAttrs = _db.ProductAttributes
                .Where(pa => pa.String != null && pa.String.Contains(searchTerm))
                .Select(pa => pa.ProductId);

            query = query.Where(p => p.Name.Contains(searchTerm) ||
                                     p.Description.Contains(searchTerm) ||
                                     productIdsFromAttrs.Contains(p.Id));

            var total = await query.CountAsync();
            var items = await query
                .Include(p => p.ProductImages)
                .Include(p => p.Category)
                .OrderBy(p => p.Name)
                .Skip(pageSize * (pageNumber - 1))
                .Take(pageSize)
                .ToListAsync();

            var dtoList = items.Select(ProductMapping.ToListItem).ToList();

            response.Result = new
            {
                Products = dtoList,
                TotalItems = total,
                TotalPages = (int)Math.Ceiling((double)total / pageSize),
                CurrentPage = pageNumber
            };
            response.StatusCode = System.Net.HttpStatusCode.OK;
            return response;
        }

        public async Task<ApiResponse> CreateAsync(ProductCreateDTO dto)
        {
            var response = new ApiResponse();

            if (!await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId))
            {
                response.IsSuccess = false;
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                response.ErrorMessages.Add("Category doesn't exist");
                return response;
            }

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                CategoryId = dto.CategoryId,
                Price = dto.Price,
                Stock = dto.Stock
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            response.Result = ProductMapping.ToDetails(product, new List<ProductAttributeDTO>());
            response.StatusCode = System.Net.HttpStatusCode.Created;
            return response;
        }

        public async Task<ApiResponse> UpdateAsync(int id, ProductUpdateDTO dto)
        {
            var response = new ApiResponse();

            var product = await _db.Products.FindAsync(id);
            if (product == null)
            {
                response.IsSuccess = false;
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                response.ErrorMessages.Add("Product not found");
                return response;
            }

            if (!await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId))
            {
                response.IsSuccess = false;
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                response.ErrorMessages.Add("Category doesn't exist");
                return response;
            }

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.CategoryId = dto.CategoryId;
            product.Price = dto.Price;
            product.Stock = dto.Stock;

            _db.Products.Update(product);
            await _db.SaveChangesAsync();

            response.StatusCode = System.Net.HttpStatusCode.NoContent;
            return response;
        }

        public async Task<ApiResponse> DeleteAsync(int id)
        {
            var response = new ApiResponse();

            var product = await _db.Products.FindAsync(id);
            if (product == null)
            {
                response.IsSuccess = false;
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                response.ErrorMessages.Add("Product not found");
                return response;
            }

            var productAttributes = _db.ProductAttributes.Where(pa => pa.ProductId == id);
            var productImages = _db.ProductImages.Where(pi => pi.ProductId == id);
            _db.ProductAttributes.RemoveRange(productAttributes);
            _db.ProductImages.RemoveRange(productImages);
            _db.Products.Remove(product);

            await _db.SaveChangesAsync();

            response.StatusCode = System.Net.HttpStatusCode.NoContent;
            return response;
        }
    }
}
