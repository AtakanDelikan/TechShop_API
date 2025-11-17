using Microsoft.EntityFrameworkCore;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services.Interfaces;

namespace TechShop_API.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;

        public CategoryService(ApplicationDbContext context)
        {
            _context = context;
        }


        // -----------------------
        // GET CATEGORY TREE
        // -----------------------
        public async Task<List<CategoryDTO>> GetCategoriesTreeAsync()
        {
            var categories = await _context.Categories.ToListAsync();
            return BuildCategoryTree(categories, null);
        }


        // -----------------------
        // GET CATEGORY BY ID
        // -----------------------
        public async Task<CategoryDetailsDTO?> GetCategoryByIdAsync(int id)
        {
            var category = await _context.Categories
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return null;

            return new CategoryDetailsDTO
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Parent = category.ParentCategory != null
                    ? new ParentCategoryDTO
                    {
                        Id = category.ParentCategory.Id,
                        Name = category.ParentCategory.Name
                    }
                    : null
            };
        }


        // -----------------------
        // SEARCH CATEGORIES
        // -----------------------
        public async Task<List<CategorySearchDTO>> SearchCategoriesAsync(string searchTerm, int count)
        {
            return await _context.Categories
                .Where(c => c.Name.Contains(searchTerm) || c.Description.Contains(searchTerm))
                .OrderBy(c => c.Name)
                .Take(count)
                .Select(c => new CategorySearchDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description
                })
                .ToListAsync();
        }


        // -----------------------
        // CREATE CATEGORY
        // -----------------------
        public async Task<CategoryDetailsDTO> CreateCategoryAsync(CategoryCreateDTO dto)
        {
            if (dto.ParentCategoryId != null)
            {
                bool parentExists = await _context.Categories.AnyAsync(c => c.Id == dto.ParentCategoryId);
                if (!parentExists)
                    throw new ArgumentException("Parent category does not exist.");
            }

            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description,
                ParentCategoryId = dto.ParentCategoryId
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return await GetCategoryByIdAsync(category.Id)
                ?? throw new Exception("Failed creating category.");
        }


        // -----------------------
        // UPDATE CATEGORY
        // -----------------------
        public async Task UpdateCategoryAsync(int id, CategoryUpdateDTO dto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                throw new KeyNotFoundException("Category not found.");

            if (dto.ParentCategoryId == id)
                throw new ArgumentException("A category cannot be its own parent.");

            if (dto.ParentCategoryId != null)
            {
                bool parentExists = await _context.Categories.AnyAsync(c => c.Id == dto.ParentCategoryId);
                if (!parentExists)
                    throw new ArgumentException("Parent category does not exist.");
            }

            category.Name = dto.Name;
            category.Description = dto.Description;
            category.ParentCategoryId = dto.ParentCategoryId;

            await _context.SaveChangesAsync();
        }


        // -----------------------
        // DELETE CATEGORY
        // -----------------------
        public async Task DeleteCategoryAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                throw new KeyNotFoundException("Category not found.");

            if (await _context.Products.AnyAsync(p => p.CategoryId == id))
                throw new InvalidOperationException("Category has products.");

            if (await _context.Categories.AnyAsync(c => c.ParentCategoryId == id))
                throw new InvalidOperationException("Category has sub-categories.");

            var attributes = _context.CategoryAttributes.Where(c => c.CategoryId == id);
            _context.CategoryAttributes.RemoveRange(attributes);

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }


        // -----------------------
        // PRIVATE HELPERS
        // -----------------------
        private List<CategoryDTO> BuildCategoryTree(List<Category> all, int? parentId)
        {
            return all
                .Where(c => c.ParentCategoryId == parentId)
                .Select(c => new CategoryDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    SubCategories = BuildCategoryTree(all, c.Id)
                }).ToList();
        }
    }
}
