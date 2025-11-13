using TechShop_API.Models;

namespace TechShop_API.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<ApiResponse> MakePaymentAsync(string userId);
    }
}
