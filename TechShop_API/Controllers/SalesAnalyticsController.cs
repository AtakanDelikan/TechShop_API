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

            _response.Result = (new
            {
                totalRevenue = new Dictionary<string, decimal>
                {
                    ["2021"] = 326485.73m,
                    ["2022"] = 451972.64m,
                    ["2023"] = 598321.47m,
                    ["2024"] = 743158.29m
                },
                totalOrders = new Dictionary<string, int>
                {
                    ["2021"] = 684,
                    ["2022"] = 912,
                    ["2023"] = 1085,
                    ["2024"] = 1243
                },
                totalItemsSold = new Dictionary<string, int>
                {
                    ["2021"] = 1783,
                    ["2022"] = 2347,
                    ["2023"] = 2891,
                    ["2024"] = 3245
                },
                uniqueCustomers = new Dictionary<string, int>
                {
                    ["2021"] = 512,
                    ["2022"] = 693,
                    ["2023"] = 821,
                    ["2024"] = 947
                },
                topSellingCategories = new Dictionary<string, int>
                {
                    ["Headphones"] = 864,
                    ["Keyboards"] = 752,
                    ["Cables"] = 687,
                    ["Mice"] = 604,
                    ["Laptops"] = 559
                },
                topRevenueByCategories = new Dictionary<string, decimal>
                {
                    ["Laptops"] = 278413.67m,
                    ["Smartphones"] = 189402.56m,
                    ["Monitors"] = 122850.88m,
                    ["Tablets"] = 93410.21m,
                    ["Desktops"] = 71465.44m
                },
                revenueOverTime = new Dictionary<string, decimal[]>
                {
                    ["2021"] = GenerateDailyValues(rng, 365, 350, 1800),
                    ["2022"] = GenerateDailyValues(rng, 365, 400, 2200),
                    ["2023"] = GenerateDailyValues(rng, 365, 500, 2800),
                    ["2024"] = GenerateDailyValues(rng, 366, 600, 3200) // Leap year
                },
                ordersOverTime = new Dictionary<string, decimal[]>
                {
                    ["2021"] = GenerateDailyValues(rng, 365, 0, 5),
                    ["2022"] = GenerateDailyValues(rng, 365, 1, 6),
                    ["2023"] = GenerateDailyValues(rng, 365, 1, 7),
                    ["2024"] = GenerateDailyValues(rng, 366, 2, 8) // Leap year
                },
            });

            return _response;
        }

        private decimal[] GenerateDailyValues(Random rng, int days, int min, int max)
        {
            return Enumerable.Range(1, days)
                .Select(_ => (decimal)rng.Next(min, max))
                .ToArray();
        }

    }
}
