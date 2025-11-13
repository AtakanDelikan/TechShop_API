using Stripe;
using TechShop_API.Services.Interfaces;

namespace TechShop_API.Services
{
    public class StripePaymentGateway : IPaymentGateway
    {
        private readonly IConfiguration _configuration;

        public StripePaymentGateway(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<(string IntentId, string ClientSecret)> CreatePaymentIntentAsync(decimal amount, string currency = "eur")
        {
            StripeConfiguration.ApiKey = _configuration["StripeSettings:SecretKey"];

            var options = new PaymentIntentCreateOptions
            {
                Amount = (int)(amount * 100),
                Currency = currency,
                PaymentMethodTypes = new List<string> { "card" },
            };

            var service = new PaymentIntentService();
            var intent = await service.CreateAsync(options);

            return (intent.Id, intent.ClientSecret);
        }
    }
}
