using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services;
using Xunit;

namespace TechShop.Tests.Services
{
    public class CategoryServiceTests
    {
        private ApplicationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task GetCategoriesTreeAsync_BuildsTreeCorrectly()
        {
            using var ctx = CreateDbContext();
            // seed categories: root -> child -> grandchild
            var root = new Category { Name = "Root" };
            ctx.Categories.Add(root);
            await ctx.SaveChangesAsync();

            var child = new Category { Name = "Child", ParentCategoryId = root.Id };
            ctx.Categories.Add(child);
            await ctx.SaveChangesAsync();

            var grand = new Category { Name = "Grand", ParentCategoryId = child.Id };
            ctx.Categories.Add(grand);
            await ctx.SaveChangesAsync();

            var svc = new CategoryService(ctx);
            var tree = await svc.GetCategoriesTreeAsync();

            Assert.NotNull(tree);
            Assert.Single(tree);
            Assert.Equal("Root", tree[0].Name);
            Assert.Single(tree[0].SubCategories);
            Assert.Equal("Child", tree[0].SubCategories[0].Name);
            Assert.Single(tree[0].SubCategories[0].SubCategories);
            Assert.Equal("Grand", tree[0].SubCategories[0].SubCategories[0].Name);
        }

        [Fact]
        public async Task GetCategoryByIdAsync_ReturnsNull_WhenNotFound()
        {
            using var ctx = CreateDbContext();
            var svc = new CategoryService(ctx);

            var result = await svc.GetCategoryByIdAsync(999);
            Assert.Null(result);
        }

        [Fact]
        public async Task GetCategoryByIdAsync_ReturnsMappedDto_WhenFound()
        {
            using var ctx = CreateDbContext();
            var parent = new Category { Name = "Parent" };
            ctx.Categories.Add(parent);
            await ctx.SaveChangesAsync();

            var c = new Category { Name = "C", Description = "desc", ParentCategoryId = parent.Id };
            ctx.Categories.Add(c);
            await ctx.SaveChangesAsync();

            var svc = new CategoryService(ctx);
            var dto = await svc.GetCategoryByIdAsync(c.Id);

            Assert.NotNull(dto);
            Assert.Equal(c.Name, dto.Name);
            Assert.Equal(c.Description, dto.Description);
            Assert.NotNull(dto.Parent);
            Assert.Equal(parent.Name, dto.Parent.Name);
        }

        [Fact]
        public async Task SearchCategoriesAsync_ReturnsMatches()
        {
            using var ctx = CreateDbContext();
            ctx.Categories.Add(new Category { Name = "Alpha", Description = "one" });
            ctx.Categories.Add(new Category { Name = "Beta", Description = "two" });
            ctx.Categories.Add(new Category { Name = "Alphabet", Description = "three" });
            await ctx.SaveChangesAsync();

            var svc = new CategoryService(ctx);
            var results = await svc.SearchCategoriesAsync("Alph", 10);

            Assert.True(results.Count >= 2);
            Assert.Contains(results, r => r.Name == "Alpha");
            Assert.Contains(results, r => r.Name == "Alphabet");
        }

        [Fact]
        public async Task CreateCategoryAsync_Creates_WhenParentNull()
        {
            using var ctx = CreateDbContext();
            var svc = new CategoryService(ctx);

            var createDto = new CategoryCreateDTO { Name = "New", Description = "d", ParentCategoryId = null };
            var created = await svc.CreateCategoryAsync(createDto);

            Assert.NotNull(created);
            Assert.Equal("New", created.Name);
            Assert.Null(created.Parent);
            // persisted
            var db = await ctx.Categories.FindAsync(created.Id);
            Assert.NotNull(db);
        }

        [Fact]
        public async Task CreateCategoryAsync_Throws_WhenParentMissing()
        {
            using var ctx = CreateDbContext();
            var svc = new CategoryService(ctx);

            var createDto = new CategoryCreateDTO { Name = "New", ParentCategoryId = 999 };
            await Assert.ThrowsAsync<ArgumentException>(() => svc.CreateCategoryAsync(createDto));
        }

        [Fact]
        public async Task UpdateCategoryAsync_UpdatesSuccessfully()
        {
            using var ctx = CreateDbContext();
            var c = new Category { Name = "Old", Description = "x" };
            ctx.Categories.Add(c);
            await ctx.SaveChangesAsync();

            var svc = new CategoryService(ctx);
            var updateDto = new CategoryUpdateDTO { Name = "New", Description = "y", ParentCategoryId = null };
            await svc.UpdateCategoryAsync(c.Id, updateDto);

            var db = await ctx.Categories.FindAsync(c.Id);
            Assert.Equal("New", db.Name);
            Assert.Equal("y", db.Description);
        }

        [Fact]
        public async Task UpdateCategoryAsync_Throws_WhenParentDoesNotExist()
        {
            using var ctx = CreateDbContext();
            var c = new Category { Name = "Cat" };
            ctx.Categories.Add(c);
            await ctx.SaveChangesAsync();

            var svc = new CategoryService(ctx);
            var updateDto = new CategoryUpdateDTO { Name = "Cat2", ParentCategoryId = 999 };

            await Assert.ThrowsAsync<ArgumentException>(() => svc.UpdateCategoryAsync(c.Id, updateDto));
        }

        [Fact]
        public async Task UpdateCategoryAsync_Throws_WhenSelfParenting()
        {
            using var ctx = CreateDbContext();
            var c = new Category { Name = "Cat" };
            ctx.Categories.Add(c);
            await ctx.SaveChangesAsync();

            var svc = new CategoryService(ctx);
            var updateDto = new CategoryUpdateDTO { Name = "Cat", ParentCategoryId = c.Id };

            await Assert.ThrowsAsync<ArgumentException>(() => svc.UpdateCategoryAsync(c.Id, updateDto));
        }

        [Fact]
        public async Task UpdateCategoryAsync_Throws_WhenNotFound()
        {
            using var ctx = CreateDbContext();
            var svc = new CategoryService(ctx);
            var dto = new CategoryUpdateDTO { Name = "X", ParentCategoryId = null };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.UpdateCategoryAsync(999, dto));
        }

        [Fact]
        public async Task DeleteCategoryAsync_Removes_WhenNoDependencies()
        {
            using var ctx = CreateDbContext();
            var c = new Category { Name = "ToDelete" };
            ctx.Categories.Add(c);
            await ctx.SaveChangesAsync();

            var svc = new CategoryService(ctx);
            await svc.DeleteCategoryAsync(c.Id);

            Assert.Null(await ctx.Categories.FindAsync(c.Id));
        }

        [Fact]
        public async Task DeleteCategoryAsync_Throws_WhenHasProducts()
        {
            using var ctx = CreateDbContext();
            var c = new Category { Name = "HasProd" };
            ctx.Categories.Add(c);
            await ctx.SaveChangesAsync();

            ctx.Products.Add(new Product { Name = "P", CategoryId = c.Id, Price = 1 });
            await ctx.SaveChangesAsync();

            var svc = new CategoryService(ctx);
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.DeleteCategoryAsync(c.Id));
        }

        [Fact]
        public async Task DeleteCategoryAsync_Throws_WhenNotFound()
        {
            using var ctx = CreateDbContext();
            var svc = new CategoryService(ctx);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.DeleteCategoryAsync(999));
        }
    }
}
