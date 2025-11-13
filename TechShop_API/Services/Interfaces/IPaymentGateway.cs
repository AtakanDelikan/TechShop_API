namespace TechShop_API.Services.Interfaces
{
    public interface IPaymentGateway
    {
        Task<(string IntentId, string ClientSecret)> CreatePaymentIntentAsync(decimal amount, string currency = "eur");
    }
}
