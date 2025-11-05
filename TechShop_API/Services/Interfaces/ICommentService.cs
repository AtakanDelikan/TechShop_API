using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using TechShop_API.Models.Dto;

namespace TechShop_API.Services.Interfaces
{
    public interface ICommentService
    {
        Task<List<CommentDTO>> GetCommentsByProductAsync(int productId);
        Task CreateCommentAsync(CommentCreateDTO dto, ClaimsPrincipal userPrincipal);
        Task DeleteCommentAsync(int id, ClaimsPrincipal userPrincipal);
    }
}
