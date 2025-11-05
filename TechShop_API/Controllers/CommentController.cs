using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services;
using TechShop_API.Services.Interfaces;

namespace TechShop_API.Controllers
{
    [Route("api/Comment")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly ICommentService _commentService;
        private readonly ApiResponse _response = new();

        public CommentController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        [HttpGet("{productId:int}")]
        public async Task<IActionResult> GetCommentsByProduct(int productId)
        {
            _response.Result = await _commentService.GetCommentsByProductAsync(productId);
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> CreateComment([FromBody] CommentCreateDTO dto)
        {
            try
            {
                await _commentService.CreateCommentAsync(dto, User);
                _response.StatusCode = HttpStatusCode.Created;
                // Wrap the response in a 201 Created ObjectResult
                return Created("", _response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                return Unauthorized(_response);
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> DeleteComment(int id)
        {
            try
            {
                await _commentService.DeleteCommentAsync(id, User);
                _response.StatusCode = HttpStatusCode.NoContent;
                return Ok(_response);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
    }
}
