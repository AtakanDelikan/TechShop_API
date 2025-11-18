using Microsoft.EntityFrameworkCore;
using System.Net;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services.Interfaces;

namespace TechShop_API.Services
{
    public class ProductAttributeService : IProductAttributeService
    {
        private readonly ApplicationDbContext _db;
        private readonly IProductAttributeValueConverter _converter;

        public ProductAttributeService(ApplicationDbContext db, IProductAttributeValueConverter converter)
        {
            _db = db;
            _converter = converter;
        }

        public async Task<ApiResponse> GetAllAsync()
        {
            var response = new ApiResponse();
            var list = await _db.ProductAttributes
                .Include(pa => pa.CategoryAttribute)
                .AsNoTracking()
                .ToListAsync();

            response.Result = list.Select(pa => MapToResponse(pa)).ToList();
            response.StatusCode = HttpStatusCode.OK;
            return response;
        }

        public async Task<ApiResponse> GetByProductAsync(int productId)
        {
            var response = new ApiResponse();

            var product = await _db.Products.FindAsync(productId);
            if (product == null)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.NotFound;
                response.ErrorMessages.Add("Product not found");
                return response;
            }

            var list = await _db.ProductAttributes
                .Where(pa => pa.ProductId == productId)
                .Include(pa => pa.CategoryAttribute)
                .AsNoTracking()
                .ToListAsync();

            response.Result = list.Select(pa => MapToResponse(pa)).ToList();
            response.StatusCode = HttpStatusCode.OK;
            return response;
        }

        public async Task<ApiResponse> CreateAsync(ProductAttributeCreateDTO dto)
        {
            var response = new ApiResponse();

            // Validate product
            var product = await _db.Products.FindAsync(dto.ProductId);
            if (product == null)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add("Product doesn't exist");
                return response;
            }

            // Validate categoryAttribute
            var catAttr = await _db.CategoryAttributes.FindAsync(dto.CategoryAttributeId);
            if (catAttr == null)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add("Category attribute doesn't exist");
                return response;
            }

            // If attribute for this product already exists -> treat as update/overwrite
            var existing = await _db.ProductAttributes
                .FirstOrDefaultAsync(pa => pa.ProductId == dto.ProductId && pa.CategoryAttributeId == dto.CategoryAttributeId);

            if (existing != null)
            {
                // reuse update logic
                _converter.ApplyValue(existing, catAttr, dto.Value);
                _db.ProductAttributes.Update(existing);
                await _db.SaveChangesAsync();

                // recalc category stats
                await RecalculateCategoryAttributeStatsAsync(catAttr.Id);

                response.Result = MapToResponse(existing);
                response.StatusCode = HttpStatusCode.OK;
                return response;
            }

            // create new
            var entity = new ProductAttribute
            {
                ProductId = dto.ProductId,
                CategoryAttributeId = dto.CategoryAttributeId
            };

            _converter.ApplyValue(entity, catAttr, dto.Value);

            _db.ProductAttributes.Add(entity);
            // update category attribute metadata (unique values / min / max)
            await _db.SaveChangesAsync();

            await UpdateCategoryAttributeStatsAfterAddAsync(catAttr.Id, entity);

            response.Result = MapToResponse(entity);
            response.StatusCode = HttpStatusCode.Created;
            return response;
        }

        public async Task<ApiResponse> UpdateAsync(ProductAttributeUpdateDTO dto)
        {
            var response = new ApiResponse();

            // Validate product
            var product = await _db.Products.FindAsync(dto.ProductId);
            if (product == null)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add("Product doesn't exist");
                return response;
            }

            var catAttr = await _db.CategoryAttributes.FindAsync(dto.CategoryAttributeId);
            if (catAttr == null)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add("Category attribute doesn't exist");
                return response;
            }

            var existing = await _db.ProductAttributes
                .FirstOrDefaultAsync(pa => pa.ProductId == dto.ProductId && pa.CategoryAttributeId == dto.CategoryAttributeId);

            if (existing == null)
            {
                // If not exist, create new (mirror old controller behavior)
                var createDto = new ProductAttributeCreateDTO
                {
                    ProductId = dto.ProductId,
                    CategoryAttributeId = dto.CategoryAttributeId,
                    Value = dto.Value
                };
                return await CreateAsync(createDto);
            }

            // update existing value
            _converter.ApplyValue(existing, catAttr, dto.Value);
            _db.ProductAttributes.Update(existing);
            await _db.SaveChangesAsync();

            // after update, recalc the category attribute stats
            await RecalculateCategoryAttributeStatsAsync(catAttr.Id);

            response.StatusCode = HttpStatusCode.NoContent;
            return response;
        }

        public async Task<ApiResponse> DeleteAsync(int id)
        {
            var response = new ApiResponse();

            var existing = await _db.ProductAttributes.FindAsync(id);
            if (existing == null)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add("Product attribute not found");
                return response;
            }

            var catAttrId = existing.CategoryAttributeId;
            _db.ProductAttributes.Remove(existing);
            await _db.SaveChangesAsync();

            // recalc category attribute stats after deletion
            await RecalculateCategoryAttributeStatsAsync(catAttrId);

            response.StatusCode = HttpStatusCode.NoContent;
            return response;
        }

        // ---- helpers ----

        private ProductAttributeResponseDTO MapToResponse(ProductAttribute pa)
        {
            var dto = new ProductAttributeResponseDTO
            {
                Id = pa.Id,
                ProductId = pa.ProductId,
                CategoryAttributeId = pa.CategoryAttributeId,
                AttributeName = pa.CategoryAttribute?.AttributeName ?? string.Empty,
                DataType = pa.CategoryAttribute?.DataType ?? TechShop_API.Utility.SD.DataTypeEnum.String,
                Value = _converter.ReadValue(pa, pa.CategoryAttribute)
            };
            return dto;
        }

        /// <summary>
        /// Update category attribute stats (min/max/unique values) when a new product attribute is created.
        /// This is optimised to avoid scanning entire table when possible.
        /// </summary>
        private async Task UpdateCategoryAttributeStatsAfterAddAsync(int categoryAttributeId, ProductAttribute added)
        {
            var catAttr = await _db.CategoryAttributes.FindAsync(categoryAttributeId);
            if (catAttr == null) return;

            switch (catAttr.DataType)
            {
                case TechShop_API.Utility.SD.DataTypeEnum.String:
                    {
                        var val = added.String;
                        if (!string.IsNullOrEmpty(val))
                        {
                            if (!catAttr.UniqueValues.Contains(val))
                            {
                                catAttr.UniqueValues.Add(val);
                                _db.CategoryAttributes.Update(catAttr);
                                await _db.SaveChangesAsync();
                            }
                        }
                        break;
                    }
                case TechShop_API.Utility.SD.DataTypeEnum.Integer:
                    {
                        if (added.Integer.HasValue)
                        {
                            var v = added.Integer.Value;
                            if (!catAttr.Min.HasValue || v < catAttr.Min) catAttr.Min = v;
                            if (!catAttr.Max.HasValue || v > catAttr.Max) catAttr.Max = v;
                            _db.CategoryAttributes.Update(catAttr);
                            await _db.SaveChangesAsync();
                        }
                        break;
                    }
                case TechShop_API.Utility.SD.DataTypeEnum.Decimal:
                    {
                        if (added.Decimal.HasValue)
                        {
                            var v = added.Decimal.Value;
                            if (!catAttr.Min.HasValue || v < catAttr.Min) catAttr.Min = v;
                            if (!catAttr.Max.HasValue || v > catAttr.Max) catAttr.Max = v;
                            _db.CategoryAttributes.Update(catAttr);
                            await _db.SaveChangesAsync();
                        }
                        break;
                    }
                case TechShop_API.Utility.SD.DataTypeEnum.Boolean:
                    {
                        // no global min/max; unique values not tracked for boolean in this design
                        break;
                    }
            }
        }

        /// <summary>
        /// Recalculate Min/Max/UniqueValues for the category attribute by scanning product attributes.
        /// Used after update and delete to ensure correctness.
        /// </summary>
        private async Task RecalculateCategoryAttributeStatsAsync(int categoryAttributeId)
        {
            var catAttr = await _db.CategoryAttributes.FindAsync(categoryAttributeId);
            if (catAttr == null) return;

            var allValues = await _db.ProductAttributes
                .Where(pa => pa.CategoryAttributeId == categoryAttributeId)
                .ToListAsync();

            // Reset
            catAttr.UniqueValues = new List<string>();
            catAttr.Min = null;
            catAttr.Max = null;

            foreach (var pa in allValues)
            {
                switch (catAttr.DataType)
                {
                    case TechShop_API.Utility.SD.DataTypeEnum.String:
                        if (!string.IsNullOrEmpty(pa.String) && !catAttr.UniqueValues.Contains(pa.String))
                            catAttr.UniqueValues.Add(pa.String);
                        break;
                    case TechShop_API.Utility.SD.DataTypeEnum.Integer:
                        if (pa.Integer.HasValue)
                        {
                            var v = pa.Integer.Value;
                            if (!catAttr.Min.HasValue || v < catAttr.Min) catAttr.Min = v;
                            if (!catAttr.Max.HasValue || v > catAttr.Max) catAttr.Max = v;
                        }
                        break;
                    case TechShop_API.Utility.SD.DataTypeEnum.Decimal:
                        if (pa.Decimal.HasValue)
                        {
                            var v = pa.Decimal.Value;
                            if (!catAttr.Min.HasValue || v < catAttr.Min) catAttr.Min = v;
                            if (!catAttr.Max.HasValue || v > catAttr.Max) catAttr.Max = v;
                        }
                        break;
                    case TechShop_API.Utility.SD.DataTypeEnum.Boolean:
                        // do nothing
                        break;
                }
            }

            _db.CategoryAttributes.Update(catAttr);
            await _db.SaveChangesAsync();
        }
    }
}
