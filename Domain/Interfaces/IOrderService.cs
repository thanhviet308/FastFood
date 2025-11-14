using FastFoodShop.Domain.Entities;

namespace FastFoodShop.Domain.Interfaces
{
    public interface IOrderService
    {
        // Cho Admin (Controller đang gọi)
        Task<(IReadOnlyList<Order> Items, int Total)> FetchAllAsync(int page, int size);
        Task<Order?> GetByIdAsync(long id);
        Task DeleteByIdAsync(long id);
        Task UpdateAsync(Order order);

        // Cho Client
        Task<List<Order>> FetchOrderByUserAsync(User user);
        Task<int> GetCartCountAsync(long userId);
    }
}
