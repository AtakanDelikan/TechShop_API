using TechShop_API.Data;
using TechShop_API.Models.Dto;
using TechShop_API.Models;
using Microsoft.EntityFrameworkCore;

namespace TechShop_API.Services
{
    public class CategoryService
    {
        private readonly ApplicationDbContext _context;

        public CategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CategoryDTO>> GetCategoriesTreeAsync()
        {
            // Fetch all categories from the database
            var categories = await _context.Categories.ToListAsync();

            // Build the tree structure
            var categoryTree = BuildCategoryTree(categories, null);
            return categoryTree;
        }

        private List<CategoryDTO> BuildCategoryTree(List<Category> allCategories, int? parentId)
        {
            return allCategories
                .Where(c => c.ParentCategoryId == parentId) // Find direct children of the current parent
                .Select(c => new CategoryDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    SubCategories = BuildCategoryTree(allCategories, c.Id) // Recursively find subcategories
                })
                .ToList();
        }
    }

}
