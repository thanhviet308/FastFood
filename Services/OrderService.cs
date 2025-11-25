using FastFoodShop.Domain.Interfaces;
using FastFoodShop.Data;
using FastFoodShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FastFoodShop.Services
{
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _context;
        public OrderService(AppDbContext context) => _context = context;

        public async Task<(IReadOnlyList<Order> Items, int Total)> FetchAllAsync(int page, int size)
        {
            var query = _context.Orders.Include(o => o.User);
            var total = await query.CountAsync();
            
            var items = await query
                .OrderBy(o => o.Id)
                .Skip(Math.Max(0, (page - 1) * size))
                .Take(size)
                .ToListAsync();

            return (items, total);
        }

        public async Task<Order?> GetByIdAsync(long id)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails!)
                .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails!)
                .ThenInclude(od => od.Variant)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task DeleteByIdAsync(long id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order != null)
            {
                _context.OrderDetails.RemoveRange(order.OrderDetails!);
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateAsync(Order order)
        {
            var current = await _context.Orders.FindAsync(order.Id);
            if (current != null)
            {
                current.Status = order.Status;
                _context.Orders.Update(current);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Order>> FetchOrderByUserAsync(User user)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .Where(o => o.User != null && o.User.Id == user.Id)
                .ToListAsync();
        }

        public async Task<int> GetCartCountAsync(long userId)
        {
            return await _context.Carts
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Sum); // hoặc CountAsync(), tuỳ logic
        }

        // Thống kê
        public async Task<decimal> GetTotalRevenueAsync()
        {
            return await _context.Orders
                .Where(o => o.Status != "CANCELLED")
                .SumAsync(o => o.TotalPrice);
        }

        public async Task<Dictionary<string, int>> GetOrdersByStatusAsync()
        {
            var orders = await _context.Orders
                .GroupBy(o => o.Status ?? "UNKNOWN")
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return orders.ToDictionary(x => x.Status, x => x.Count);
        }

        public async Task<int> GetOrdersCountByStatusAsync(string status)
        {
            return await _context.Orders
                .CountAsync(o => o.Status == status);
        }

        public async Task<decimal[]> GetMonthlyRevenueAsync(int year)
        {
            // Mặc định 12 tháng = 0
            var result = new decimal[12];

            var data = await _context.Orders
                .Where(o => o.Status != "CANCELLED" && o.CreatedAt.Year == year)
                .GroupBy(o => o.CreatedAt.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Total = g.Sum(x => x.TotalPrice)
                })
                .ToListAsync();

            foreach (var item in data)
            {
                if (item.Month >= 1 && item.Month <= 12)
                {
                    result[item.Month - 1] = item.Total;
                }
            }

            return result;
        }

        public async Task UpdatePaymentStatusAsync(long orderId, string paymentStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.PaymentStatus = paymentStatus;
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
            }
        }
    }
}
