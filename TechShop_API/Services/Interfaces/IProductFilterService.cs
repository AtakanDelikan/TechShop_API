using TechShop_API.Models;

namespace TechShop_API.Services.Interfaces
{
    public interface IProductFilterService
    {
        /// <summary>
        /// Apply filters (category, price, attributes, search) to an IQueryable<Product>.
        /// Returns modified IQueryable (no execution).
        /// </summary>
        IQueryable<Product> ApplyFilters(IQueryable<Product> source, Dictionary<string, string> filters);
    }
}
