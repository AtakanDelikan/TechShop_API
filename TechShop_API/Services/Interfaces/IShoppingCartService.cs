using TechShop_API.Models;

namespace TechShop_API.Services.Interfaces
{
    public interface IShoppingCartService
    {
        Task<ShoppingCart> GetShoppingCartAsync(string userId);
        Task AddOrUpdateItemAsync(string userId, int productId, int updateQuantityBy);
        Task DeleteShoppingCartAsync(int id);
    }
}
