using TechShop_API.Models;
using TechShop_API.Models.Dto;

namespace TechShop_API.Services.Interfaces
{
    public interface IOrderService
    {
        Task<object> GetAllOrdersAsync(int pageNumber, int pageSize, string status);
        Task<IEnumerable<OrderHeader>> GetOrdersByUserAsync(string userId);
        Task<OrderHeader?> GetOrderByIdAsync(int id);
        Task<OrderHeader> CreateOrderAsync(OrderHeaderCreateDTO orderHeaderDTO);
        Task<bool> UpdateOrderAsync(int id, OrderHeaderUpdateDTO orderHeaderUpdateDTO);
    }
}
