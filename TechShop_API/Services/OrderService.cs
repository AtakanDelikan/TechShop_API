using Microsoft.EntityFrameworkCore;
using System.Net;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services.Interfaces;
using TechShop_API.Utility;

namespace TechShop_API.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _db;

        public OrderService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<object> GetAllOrdersAsync(int pageNumber, int pageSize, string status)
        {
            IQueryable<OrderHeader> orderQuery = _db.OrderHeaders;

            if (!string.IsNullOrEmpty(status))
                orderQuery = orderQuery.Where(u => u.Status == status);

            var totalOrders = await orderQuery.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalOrders / pageSize);

            var orderHeaders = await orderQuery
                .OrderByDescending(u => u.OrderDate)
                .Skip(pageSize * (pageNumber - 1))
                .Take(pageSize)
                .Include(u => u.OrderDetails)
                .ThenInclude(u => u.Product)
                .ToListAsync();

            return new
            {
                OrderHeaders = orderHeaders,
                TotalOrders = totalOrders,
                TotalPages = totalPages,
                CurrentPage = pageNumber
            };
        }

        public async Task<IEnumerable<OrderHeader>> GetOrdersByUserAsync(string userId)
        {
            return await _db.OrderHeaders
                .Where(u => u.ApplicationUserId == userId)
                .OrderByDescending(u => u.OrderDate)
                .Include(u => u.OrderDetails)
                .ThenInclude(u => u.Product)
                .ToListAsync();
        }

        public async Task<OrderHeader?> GetOrderByIdAsync(int id)
        {
            return await _db.OrderHeaders
                .Include(u => u.OrderDetails)
                .ThenInclude(u => u.Product)
                .FirstOrDefaultAsync(u => u.OrderHeaderId == id);
        }

        public async Task<OrderHeader> CreateOrderAsync(OrderHeaderCreateDTO orderHeaderDTO)
        {
            var order = new OrderHeader
            {
                ApplicationUserId = orderHeaderDTO.ApplicationUserId,
                PickupEmail = orderHeaderDTO.PickupEmail,
                PickupName = orderHeaderDTO.PickupName,
                PickupPhoneNumber = orderHeaderDTO.PickupPhoneNumber,
                OrderTotal = orderHeaderDTO.OrderTotal,
                OrderDate = DateTime.Now,
                StripePaymentIntentID = orderHeaderDTO.StripePaymentIntentID,
                TotalItems = orderHeaderDTO.TotalItems,
                Status = string.IsNullOrEmpty(orderHeaderDTO.Status)
                    ? SD.status_pending
                    : orderHeaderDTO.Status,
            };

            _db.OrderHeaders.Add(order);
            await _db.SaveChangesAsync();

            foreach (var detail in orderHeaderDTO.OrderDetailsDTO)
            {
                var orderDetail = new OrderDetail
                {
                    OrderHeaderId = order.OrderHeaderId,
                    ItemName = detail.ItemName,
                    ProductId = detail.ProductId,
                    Price = detail.Price,
                    Quantity = detail.Quantity
                };
                _db.OrderDetails.Add(orderDetail);
            }

            await _db.SaveChangesAsync();
            return order;
        }

        public async Task<bool> UpdateOrderAsync(int id, OrderHeaderUpdateDTO orderHeaderUpdateDTO)
        {
            var order = await _db.OrderHeaders.FirstOrDefaultAsync(u => u.OrderHeaderId == id);
            if (order == null) return false;

            if (!string.IsNullOrEmpty(orderHeaderUpdateDTO.PickupName))
                order.PickupName = orderHeaderUpdateDTO.PickupName;
            if (!string.IsNullOrEmpty(orderHeaderUpdateDTO.PickupEmail))
                order.PickupEmail = orderHeaderUpdateDTO.PickupEmail;
            if (!string.IsNullOrEmpty(orderHeaderUpdateDTO.PickupPhoneNumber))
                order.PickupPhoneNumber = orderHeaderUpdateDTO.PickupPhoneNumber;
            if (!string.IsNullOrEmpty(orderHeaderUpdateDTO.Status))
                order.Status = orderHeaderUpdateDTO.Status;
            if (!string.IsNullOrEmpty(orderHeaderUpdateDTO.StripePaymentIntentID))
                order.StripePaymentIntentID = orderHeaderUpdateDTO.StripePaymentIntentID;

            await _db.SaveChangesAsync();
            return true;
        }
    }
}
