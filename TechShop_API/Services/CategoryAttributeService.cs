using Microsoft.EntityFrameworkCore;
using System.Net;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services.Interfaces;
using TechShop_API.Utility;

namespace TechShop_API.Services
{
    public class CategoryAttributeService : ICategoryAttributeService
    {
        private readonly ApplicationDbContext _db;

        public CategoryAttributeService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<CategoryAttribute>> GetAllAsync()
        {
            return await _db.CategoryAttributes.ToListAsync();
        }

        public async Task<object?> GetCategoryAttributeDetailsAsync(int categoryId)
        {
            var category = await _db.Categories
                .Include(x => x.CategoryAttributes)
                .FirstOrDefaultAsync(x => x.Id == categoryId);

            if (category == null)
                return null;

            decimal maxPrice = await _db.Products
                .Where(p => p.CategoryId == categoryId)
                .MaxAsync(p => (decimal?)p.Price) ?? 0;

            return new
            {
                Attributes = category.CategoryAttributes.ToList(),
                MaxPrice = decimal.ToDouble(maxPrice)
            };
        }

        public async Task<ApiResponse> CreateAsync(CategoryAttributeCreateDTO dto)
        {
            var response = new ApiResponse();

            try
            {
                // Validate category exists
                bool categoryExists = await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId);
                if (!categoryExists)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.ErrorMessages.Add("Category does not exist.");
                    return response;
                }

                // Validate enum
                if (!Enum.IsDefined(typeof(SD.DataTypeEnum), dto.DataType))
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.ErrorMessages.Add("Invalid data type.");
                    return response;
                }

                var entity = new CategoryAttribute
                {
                    CategoryId = dto.CategoryId,
                    AttributeName = dto.AttributeName,
                    DataType = dto.DataType
                };

                _db.CategoryAttributes.Add(entity);
                await _db.SaveChangesAsync();

                response.Result = entity;
                response.StatusCode = HttpStatusCode.Created;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessages.Add(ex.Message);
            }

            return response;
        }

        public async Task<ApiResponse> UpdateAsync(int id, CategoryAttributeUpdateDTO dto)
        {
            var response = new ApiResponse();

            try
            {
                var entity = await _db.CategoryAttributes.FindAsync(id);
                if (entity == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("CategoryAttribute not found.");
                    return response;
                }

                entity.AttributeName = dto.AttributeName;

                if (dto.DataType.HasValue && dto.DataType != entity.DataType)
                {
                    var attrs = _db.ProductAttributes
                        .Where(pa => pa.CategoryAttributeId == entity.Id);

                    _db.ProductAttributes.RemoveRange(attrs);
                    entity.DataType = dto.DataType.Value;
                }

                await _db.SaveChangesAsync();
                response.StatusCode = HttpStatusCode.NoContent;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessages.Add(ex.Message);
            }

            return response;
        }

        public async Task<ApiResponse> DeleteAsync(int id)
        {
            var response = new ApiResponse();

            try
            {
                var entity = await _db.CategoryAttributes.FindAsync(id);
                if (entity == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("CategoryAttribute not found.");
                    return response;
                }

                _db.CategoryAttributes.Remove(entity);
                await _db.SaveChangesAsync();

                response.StatusCode = HttpStatusCode.NoContent;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessages.Add(ex.Message);
            }

            return response;
        }
    }
}
