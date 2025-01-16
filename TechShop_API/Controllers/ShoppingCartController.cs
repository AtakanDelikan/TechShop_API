using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using TechShop_API.Data;
using TechShop_API.Models;

namespace TechShop_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShoppingCartController : ControllerBase
    {
        protected ApiResponse _response;
        private readonly ApplicationDbContext _db;
        public ShoppingCartController(ApplicationDbContext db)
        {
            _response = new ApiResponse();
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse>> GetShoppingCart(string userId)
        {
            try
            {
                ShoppingCart shoppingCart;
                if (string.IsNullOrEmpty(userId))
                {
                    shoppingCart = new();
                }
                else
                {
                    shoppingCart = _db.ShoppingCarts
                        .Include(u => u.CartItems).ThenInclude(u => u.Laptop)
                        .FirstOrDefault(u => u.UserId == userId);
                }
                if (shoppingCart == null) {
                    _response.Result = new ShoppingCart();
                } else if (shoppingCart.CartItems != null && shoppingCart.CartItems.Count > 0)
                {
                    shoppingCart.CartTotal = shoppingCart.CartItems.Sum(u => u.Quantity*u.Laptop.Price);
                    _response.Result = shoppingCart;
                }

                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                    = new List<string>() { ex.ToString() };
                _response.StatusCode = HttpStatusCode.BadRequest;
            }
            return _response;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse>> AddOrUpdateItemInCart(string userId, int laptopId, int updateQuantityBy)
        {
            // 1 shopping cat per 1 user
            // if updatequantityby is 0, item will be removed

            // user adds a new item to a new shopping cart for the first time
            // user adds a new item to an existing shopping cart
            // user updates an existing item count
            // user removes an existing item

            ShoppingCart shoppingCart = _db.ShoppingCarts.Include(u => u.CartItems).FirstOrDefault(u => u.UserId == userId);
            Laptop laptop = _db.Laptops.FirstOrDefault(u => u.Id == laptopId);
            if (laptop == null)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                return BadRequest();
            }
            if (shoppingCart == null && updateQuantityBy > 0)
            {
                // create a shopping cart & add cart item
                ShoppingCart newCart = new() { UserId = userId };
                _db.ShoppingCarts.Add(newCart);
                _db.SaveChanges();

                CartItem newCartItem = new()
                {
                    LaptopId = laptopId,
                    Quantity = updateQuantityBy,
                    ShoppingCartId = newCart.Id,
                    Laptop = null,
                };
                _db.CartItems.Add(newCartItem);
                _db.SaveChanges();
            }
            else
            {
                // shopping cart exist
                CartItem cartItemInCart = shoppingCart.CartItems.FirstOrDefault(u => u.LaptopId == laptopId);
                if (cartItemInCart == null)
                {
                    // item does not exist in the current cart
                    CartItem newCartItem = new()
                    {
                        LaptopId = laptopId,
                        Quantity = updateQuantityBy,
                        ShoppingCartId = shoppingCart.Id,
                        Laptop=null,
                    };
                    _db.CartItems.Add(newCartItem);
                    _db.SaveChanges();
                }
                else
                {
                    // item already exists in the cart, so update quantity
                    int newQuantity = cartItemInCart.Quantity + updateQuantityBy;
                    if (updateQuantityBy == 0 || newQuantity <= 0)
                    {
                        // remove cart item from cart & if it was the only item, remove cart also
                        _db.CartItems.Remove(cartItemInCart);
                        if (shoppingCart.CartItems.Count() == 1)
                        {
                            _db.ShoppingCarts.Remove(shoppingCart);
                        }
                        _db.SaveChanges();
                    }
                    else
                    {
                        cartItemInCart.Quantity = newQuantity;
                        _db.SaveChanges();
                    }
                }
            }    

            return _response;
        }
    }
}
