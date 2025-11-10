using TechShop_API.Models.Dto;

namespace TechShop_API.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDTO> LoginAsync(LoginRequestDTO model);
        Task<bool> RegisterAsync(RegisterRequestDTO model);
        Task<UserDetailsDTO> GetUserDataAsync(string userId);
        Task<bool> UpdateUserDataAsync(string userId, UserDetailsDTO model);
    }
}
