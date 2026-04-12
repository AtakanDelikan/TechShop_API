using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Utility;

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
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<ActionResult<ApiResponse>> SalesAnalytics()
        {
            try
            {
                // YEARLY AGGREGATES
                var yearlyStats = await _db.OrderHeaders
                    .Where(o => o.Status == SD.status_delivered)
                    .GroupBy(o => o.OrderDate.Year)
                    .Select(g => new
                    {
                        Year = g.Key.ToString(),
                        TotalRevenue = g.Sum(o => o.OrderTotal),
                        TotalOrders = g.Count(),
                        TotalItemsSold = g.Sum(o => o.TotalItems),
                        UniqueCustomers = g.Select(o => o.ApplicationUserId).Distinct().Count()
                    }).ToListAsync();

                // CATEGORY AGGREGATES
                var categoryStats = await _db.OrderDetails
                    .Where(od => od.OrderHeader.Status == SD.status_delivered)
                    .GroupBy(od => od.Product.Category.Name)
                    .Select(g => new
                    {
                        CategoryName = g.Key ?? "Unknown",
                        ItemsSold = g.Sum(od => od.Quantity),
                        Revenue = g.Sum(od => od.Price * od.Quantity)
                    }).ToListAsync();

                // DAILY AGGREGATES
                var dailyStats = await _db.OrderHeaders
                    .Where(o => o.Status == SD.status_delivered)
                    .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.DayOfYear })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        DayOfYear = g.Key.DayOfYear,
                        DailyRevenue = g.Sum(o => o.OrderTotal),
                        DailyOrders = g.Count()
                    }).ToListAsync();

                // --- DATA SHAPING FOR FRONTEND ---

                // Format Daily Arrays
                var revenueOverTime = new Dictionary<string, decimal[]>();
                var ordersOverTime = new Dictionary<string, decimal[]>();
                var activeYears = dailyStats.Select(d => d.Year).Distinct().ToList();

                foreach (var year in activeYears)
                {
                    int daysInYear = DateTime.IsLeapYear(year) ? 366 : 365;
                    var revArray = new decimal[daysInYear];
                    var ordArray = new decimal[daysInYear];

                    var statsForYear = dailyStats.Where(d => d.Year == year);
                    foreach (var stat in statsForYear)
                    {
                        // DayOfYear is 1-based, Arrays are 0-based
                        revArray[stat.DayOfYear - 1] = stat.DailyRevenue;
                        ordArray[stat.DayOfYear - 1] = stat.DailyOrders;
                    }

                    revenueOverTime[year.ToString()] = revArray;
                    ordersOverTime[year.ToString()] = ordArray;
                }

                _response.Result = new
                {
                    totalRevenue = yearlyStats.ToDictionary(x => x.Year, x => x.TotalRevenue),
                    totalOrders = yearlyStats.ToDictionary(x => x.Year, x => x.TotalOrders),
                    totalItemsSold = yearlyStats.ToDictionary(x => x.Year, x => x.TotalItemsSold),
                    uniqueCustomers = yearlyStats.ToDictionary(x => x.Year, x => x.UniqueCustomers),

                    topSellingCategories = categoryStats
                        .OrderByDescending(c => c.ItemsSold)
                        .Take(5)
                        .ToDictionary(c => c.CategoryName, c => c.ItemsSold),

                    topRevenueByCategories = categoryStats
                        .OrderByDescending(c => c.Revenue)
                        .Take(5)
                        .ToDictionary(c => c.CategoryName, c => c.Revenue),

                    revenueOverTime,
                    ordersOverTime
                };

                _response.StatusCode = System.Net.HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages.Add($"Error generating analytics: {ex.Message}");
                return BadRequest(_response);
            }
        }
    }
}