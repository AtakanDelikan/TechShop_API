using TechShop_API.Models;
using TechShop_API.Models.Dto;

namespace TechShop_API.Services.Interfaces
{
    public interface ICategoryAttributeService
    {
        Task<IEnumerable<CategoryAttribute>> GetAllAsync();
        Task<object?> GetCategoryAttributeDetailsAsync(int categoryId);
        Task<ApiResponse> CreateAsync(CategoryAttributeCreateDTO dto);
        Task<ApiResponse> UpdateAsync(int id, CategoryAttributeUpdateDTO dto);
        Task<ApiResponse> DeleteAsync(int id);
    }
}
