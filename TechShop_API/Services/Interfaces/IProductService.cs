using TechShop_API.Models;
using TechShop_API.Models.Dto;

namespace TechShop_API.Services.Interfaces
{
    public interface IProductService
    {
        Task<ApiResponse> GetAllAsync();
        Task<ApiResponse> GetByIdAsync(int id);
        Task<ApiResponse> GetByCategoryAsync(int categoryId, int pageNumber, int pageSize);
        Task<ApiResponse> FilterProductsAsync(Dictionary<string, string> filters, int pageNumber, int pageSize);
        Task<ApiResponse> SearchProductsAsync(string searchTerm, int pageNumber, int pageSize);
        Task<ApiResponse> CreateAsync(ProductCreateDTO dto);
        Task<ApiResponse> UpdateAsync(int id, ProductUpdateDTO dto);
        Task<ApiResponse> DeleteAsync(int id);
    }
}
