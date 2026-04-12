using TechShop_API.Models;
using TechShop_API.Models.Dto;

namespace TechShop_API.Services.Interfaces
{
    public interface IShoppingCartService
    {
        Task<ShoppingCartDTO> GetShoppingCartAsync(string userId);
        Task AddOrUpdateItemAsync(string userId, int productId, int updateQuantityBy);
        Task DeleteShoppingCartAsync(int id);
    }
}
