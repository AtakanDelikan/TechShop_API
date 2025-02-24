using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Utility;

namespace TechShop_API.Controllers
{
    [Route("api/Comment")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private ApiResponse _response;
        private readonly UserManager<ApplicationUser> _userManager;
        public CommentController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _response = new ApiResponse();
            _userManager = userManager;
        }

        [HttpGet("{productId:int}")]
        public async Task<IActionResult> GetCommentByProduct(int productId)
        {
            var comments = await _db.Comments
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

            _response.Result = comments;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> CreateComment([FromBody] CommentCreateDTO commentDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return Unauthorized();
            try
            {
                var comment = new Comment
                {
                    ProductId = commentDto.ProductId,
                    ApplicationUserId = user.Id,
                    Content = commentDto.Content,
                    Rating= commentDto.Rating,
                    CreatedAt = DateTime.UtcNow,
                };

                _db.Comments.Add(comment);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                    = new List<string>() { ex.ToString() };
            }

            return _response;
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var comment = await _db.Comments.FindAsync(id);

            if (comment == null) return NotFound();
            if (comment.ApplicationUserId != user.Id) return Forbid();

            _db.Comments.Remove(comment);
            await _db.SaveChangesAsync();

            return Ok();
        }

    }
}
