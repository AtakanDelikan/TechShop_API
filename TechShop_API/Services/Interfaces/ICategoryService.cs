using TechShop_API.Models;
using TechShop_API.Models.Dto;

namespace TechShop_API.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<List<CategoryDTO>> GetCategoriesTreeAsync();
        Task<CategoryDetailsDTO?> GetCategoryByIdAsync(int id);
        Task<CategoryDetailsDTO> CreateCategoryAsync(CategoryCreateDTO dto);
        Task UpdateCategoryAsync(int id, CategoryUpdateDTO dto);
        Task DeleteCategoryAsync(int id);
    }
}
