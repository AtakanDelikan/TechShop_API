using TechShop_API.Models;
using TechShop_API.Models.Dto;

namespace TechShop_API.Services.Interfaces
{
    public interface IBulkImportService
    {
        Task<ApiResponse> ImportCategoriesAsync(Stream fileStream);
        Task<ApiResponse> ImportCategoryAttributesAsync(Stream fileStream);
        Task<ApiResponse> ImportProductsAsync(Stream fileStream);
    }
}
