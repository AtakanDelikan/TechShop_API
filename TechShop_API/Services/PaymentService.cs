using Microsoft.EntityFrameworkCore;
using System.Net;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Services.Interfaces;

namespace TechShop_API.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _db;
        private readonly IPaymentGateway _paymentGateway;

        public PaymentService(ApplicationDbContext db, IPaymentGateway paymentGateway)
        {
            _db = db;
            _paymentGateway = paymentGateway;
        }

        public async Task<ApiResponse> MakePaymentAsync(string userId)
        {
            var response = new ApiResponse();

            var shoppingCart = await _db.ShoppingCarts
                .Include(u => u.CartItems)
                .ThenInclude(u => u.Product)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (shoppingCart == null || shoppingCart.CartItems == null || !shoppingCart.CartItems.Any())
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                response.IsSuccess = false;
                response.ErrorMessages.Add("Shopping cart is empty or not found.");
                return response;
            }

            // Calculate total
            shoppingCart.CartTotal = shoppingCart.CartItems.Sum(u => u.Quantity * u.Product.Price);

            // Create payment intent
            var (intentId, clientSecret) = await _paymentGateway.CreatePaymentIntentAsync(shoppingCart.CartTotal);

            // Update cart
            shoppingCart.StripePaymentIntentId = intentId;
            shoppingCart.ClientSecret = clientSecret;

            response.Result = shoppingCart;
            response.StatusCode = HttpStatusCode.OK;
            response.IsSuccess = true;

            return response;
        }
    }
}
