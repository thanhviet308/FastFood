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
                .OrderByDescending(o => o.Id)
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
    }
}
