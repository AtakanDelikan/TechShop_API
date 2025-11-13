using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Services;
using TechShop_API.Services.Interfaces;
using Xunit;

namespace TechShop.Tests.Services
{
    public class ShoppingCartServiceTests
    {
        private ApplicationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // unique for each test
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task GetShoppingCartAsync_ReturnsEmptyCart_WhenUserNotFound()
        {
            using var context = CreateDbContext();
            IShoppingCartService service = new ShoppingCartService(context);

            var cart = await service.GetShoppingCartAsync("nonexistent-user");

            Assert.NotNull(cart);
            Assert.Empty(cart.CartItems);
        }

        [Fact]
        public async Task AddOrUpdateItemAsync_AddsNewItem_WhenCartDoesNotExist()
        {
            using var context = CreateDbContext();
            context.Products.Add(new Product { Id = 1, Name = "Laptop", Price = 1000 });
            await context.SaveChangesAsync();

            IShoppingCartService service = new ShoppingCartService(context);

            await service.AddOrUpdateItemAsync("user1", 1, 2);

            var cart = context.ShoppingCarts.Include(c => c.CartItems).FirstOrDefault();
            Assert.NotNull(cart);
            Assert.Single(cart.CartItems);
            Assert.Equal(2, cart.CartItems.First().Quantity);
        }

        [Fact]
        public async Task DeleteShoppingCartAsync_RemovesCartAndItems()
        {
            using var context = CreateDbContext();
            var cart = new ShoppingCart { UserId = "user1" };
            context.ShoppingCarts.Add(cart);
            context.CartItems.Add(new CartItem { ShoppingCartId = cart.Id, ProductId = 1, Quantity = 1 });
            await context.SaveChangesAsync();

            IShoppingCartService service = new ShoppingCartService(context);
            await service.DeleteShoppingCartAsync(cart.Id);

            Assert.Empty(context.ShoppingCarts);
            Assert.Empty(context.CartItems);
        }
    }
}
