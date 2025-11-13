using Microsoft.AspNetCore.Mvc;
using System.Net;
using TechShop_API.Models;
using TechShop_API.Services.Interfaces;

namespace TechShop_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse>> MakePayment(string userId)
        {
            try
            {
                var response = await _paymentService.MakePaymentAsync(userId);

                if (!response.IsSuccess)
                    return BadRequest(response);

                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessages = new List<string> { ex.Message }
                };

                return StatusCode((int)HttpStatusCode.InternalServerError, errorResponse);
            }
        }
    }
}
