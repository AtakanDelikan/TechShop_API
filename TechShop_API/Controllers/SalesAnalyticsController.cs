using Microsoft.AspNetCore.Mvc;
using TechShop_API.Data;
using TechShop_API.Models;

namespace TechShop_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesAnalyticsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private ApiResponse _response;
        public SalesAnalyticsController(ApplicationDbContext db)
        {
            _db = db;
            _response = new ApiResponse();
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse>> SalesAnalytics()
        {
            var rng = new Random();
            // Generate time series data (flat arrays)

            var revenueOverTime = GenerateYearlyTimeSeries(rng, 100, 300, 150, 350);
            var ordersOverTime = GenerateYearlyTimeSeries(rng, 1, 10, 3, 12);

            _response.Result = (new
            {
                totalRevenue = new Dictionary<string, decimal>
                {
                    ["2021"] = 125430m,
                    ["2022"] = 158920m,
                    ["2023"] = 194560m,
                    ["2024"] = 227380m
                },
                totalOrders = new Dictionary<string, int>
                {
                    ["2021"] = 213,
                    ["2022"] = 261,
                    ["2023"] = 279,
                    ["2024"] = 317
                },
                totalItemsSold = new Dictionary<string, int>
                {
                    ["2021"] = 842,
                    ["2022"] = 1287,
                    ["2023"] = 1753,
                    ["2024"] = 2142
                },
                uniqueCustomers = new Dictionary<string, int>
                {
                    ["2021"] = 83,
                    ["2022"] = 131,
                    ["2023"] = 207,
                    ["2024"] = 246
                },
                topSellingCategories = new Dictionary<string, int>
                {
                    ["Keyboards"] = 2235,
                    ["PCs"] = 1247,
                    ["Monitors"] = 992,
                    ["Laptops"] = 815,
                    ["Mice"] = 735
                },
                topRevenueByCategories = new Dictionary<string, decimal>
                {
                    ["Keyboards"] = 4120m,
                    ["PCs"] = 3475m,
                    ["Monitors"] = 3350m,
                    ["Laptops"] = 2975m,
                    ["Mice"] = 2080m
                },
                revenueOverTime,
                ordersOverTime,
            });

            return _response;
        }


        private Dictionary<string, decimal[]> GenerateYearlyTimeSeries(Random rng, int minOld, int maxOld, int minRecent, int maxRecent)
        {
            return new Dictionary<string, decimal[]>
            {
                ["2021"] = GenerateDailyValues(rng, 365, minOld, maxOld),
                ["2022"] = GenerateDailyValues(rng, 365, minOld, maxOld),
                ["2023"] = GenerateDailyValues(rng, 365, minOld + 20, maxOld + 30),
                ["2024"] = GenerateDailyValues(rng, 366, minRecent, maxRecent) // Leap year
            };
        }

        private decimal[] GenerateDailyValues(Random rng, int days, int min, int max)
        {
            return Enumerable.Range(1, days)
                .Select(_ => (decimal)rng.Next(min, max))
                .ToArray();
        }

    }
}
