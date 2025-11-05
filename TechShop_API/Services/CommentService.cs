using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services.Interfaces;

namespace TechShop_API.Services
{
    public class CommentService : ICommentService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public CommentService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<List<CommentDTO>> GetCommentsByProductAsync(int productId)
        {
            return await _db.Comments
                .Where(c => c.ProductId == productId)
                .Include(c => c.User)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentDTO
                {
                    CommentId = c.Id,
                    ProductId = c.ProductId,
                    Content = c.Content,
                    Rating = c.Rating,
                    UserName = c.User.Name,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();
        }

        public async Task CreateCommentAsync(CommentCreateDTO dto, ClaimsPrincipal userPrincipal)
        {
            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                throw new UnauthorizedAccessException("User not found");

            var comment = new Comment
            {
                ProductId = dto.ProductId,
                ApplicationUserId = user.Id,
                Content = dto.Content,
                Rating = dto.Rating,
                CreatedAt = DateTime.UtcNow,
            };

            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();

            await RecalculateRatingAsync(dto.ProductId);
        }

        public async Task DeleteCommentAsync(int id, ClaimsPrincipal userPrincipal)
        {
            var user = await _userManager.GetUserAsync(userPrincipal);
            var comment = await _db.Comments.FindAsync(id);

            if (comment == null)
                throw new KeyNotFoundException("Comment not found");

            if (comment.ApplicationUserId != user.Id)
                throw new UnauthorizedAccessException("You can only delete your own comments");

            _db.Comments.Remove(comment);
            await _db.SaveChangesAsync();
            await RecalculateRatingAsync(comment.ProductId);
        }

        private async Task RecalculateRatingAsync(int productId)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null) return;

            var ratings = await _db.Comments
                .Where(c => c.ProductId == productId)
                .Select(c => c.Rating)
                .ToListAsync();

            product.Rating = ratings.Any() ? ratings.Average() : 0;
            _db.Products.Update(product);
            await _db.SaveChangesAsync();
        }
    }
}