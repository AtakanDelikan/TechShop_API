using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services.Interfaces;

namespace TechShop_API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ApiResponse _response = new();

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO model)
        {
            try
            {
                var result = await _authService.LoginAsync(model);
                _response.Result = result;
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                _response.StatusCode = HttpStatusCode.BadRequest;
                return BadRequest(_response);
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO model)
        {
            try
            {
                var success = await _authService.RegisterAsync(model);
                _response.StatusCode = success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                _response.IsSuccess = success;
                return Ok(_response);
            }
            catch (InvalidOperationException ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                _response.StatusCode = HttpStatusCode.BadRequest;
                return BadRequest(_response);
            }
        }

        [HttpGet("userdata")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> GetUserData()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                _response.Result = await _authService.GetUserDataAsync(userId);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }

        [HttpPut("userdata")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> UpdateUserDetails([FromForm] UserDetailsDTO model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                await _authService.UpdateUserDataAsync(userId, model);
                _response.StatusCode = HttpStatusCode.NoContent;
                return Ok(_response);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }
    }
}
