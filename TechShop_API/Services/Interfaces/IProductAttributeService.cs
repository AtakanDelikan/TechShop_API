using TechShop_API.Models.Dto;
using TechShop_API.Models;

namespace TechShop_API.Services.Interfaces
{
    public interface IProductAttributeService
    {
        Task<ApiResponse> GetAllAsync();
        Task<ApiResponse> GetByProductAsync(int productId);
        Task<ApiResponse> CreateAsync(ProductAttributeCreateDTO dto);
        Task<ApiResponse> UpdateAsync(ProductAttributeUpdateDTO dto);
        Task<ApiResponse> DeleteAsync(int id);
    }
}
