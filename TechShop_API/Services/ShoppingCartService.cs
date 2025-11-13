using Microsoft.EntityFrameworkCore;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Services.Interfaces;

namespace TechShop_API.Services
{
    public class ShoppingCartService : IShoppingCartService
    {
        private readonly ApplicationDbContext _db;

        public ShoppingCartService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ShoppingCart> GetShoppingCartAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return new ShoppingCart();

            var shoppingCart = await _db.ShoppingCarts
                .Include(u => u.CartItems)
                .ThenInclude(u => u.Product)
                .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (shoppingCart != null && shoppingCart.CartItems != null && shoppingCart.CartItems.Any())
            {
                shoppingCart.CartTotal = shoppingCart.CartItems.Sum(ci => ci.Quantity * ci.Product.Price);
            }

            return shoppingCart ?? new ShoppingCart();
        }

        public async Task AddOrUpdateItemAsync(string userId, int productId, int updateQuantityBy)
        {
            // 1 shopping cat per 1 user
            // if updatequantityby is 0, item will be removed

            // user adds a new item to a new shopping cart for the first time
            // user adds a new item to an existing shopping cart
            // user updates an existing item count
            // user removes an existing item
            var shoppingCart = await _db.ShoppingCarts
                .Include(u => u.CartItems)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            var product = await _db.Products.FindAsync(productId);
            if (product == null)
                throw new Exception("Product not found");

            if (shoppingCart == null && updateQuantityBy > 0)
            {
                shoppingCart = new ShoppingCart { UserId = userId };
                _db.ShoppingCarts.Add(shoppingCart);
                await _db.SaveChangesAsync();
            }

            var cartItem = shoppingCart?.CartItems?.FirstOrDefault(ci => ci.ProductId == productId);

            if (cartItem == null && updateQuantityBy > 0)
            {
                _db.CartItems.Add(new CartItem
                {
                    ProductId = productId,
                    Quantity = updateQuantityBy,
                    ShoppingCartId = shoppingCart.Id
                });
            }
            else if (cartItem != null)
            {
                int newQuantity = cartItem.Quantity + updateQuantityBy;
                if (updateQuantityBy == 0 || newQuantity <= 0)
                {
                    _db.CartItems.Remove(cartItem);
                    if (shoppingCart.CartItems.Count == 1)
                        _db.ShoppingCarts.Remove(shoppingCart);
                }
                else
                {
                    cartItem.Quantity = newQuantity;
                }
            }

            await _db.SaveChangesAsync();
        }

        public async Task DeleteShoppingCartAsync(int id)
        {
            var shoppingCart = await _db.ShoppingCarts
                .Include(sc => sc.CartItems)
                .FirstOrDefaultAsync(sc => sc.Id == id);

            if (shoppingCart == null)
                throw new Exception("Shopping cart not found");

            _db.CartItems.RemoveRange(shoppingCart.CartItems);
            _db.ShoppingCarts.Remove(shoppingCart);
            await _db.SaveChangesAsync();
        }
    }
}
