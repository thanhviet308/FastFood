using FastFoodShop.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace FastFoodShop.Domain.Interfaces
{
    public class ProductCriteriaDto
    {
        public List<string>? Target { get; set; }
        public List<string>? Factory { get; set; }
        public List<string>? Price { get; set; }   // "duoi-200-nghin", "200-500-nghin", "tren-500-nghin"
    }

    public interface IProductService
    {
        Task<Product> CreateAsync(Product pr);

        Task<(IReadOnlyList<Product> Items, int Total)> FetchAsync(int page, int size);
        Task<(IReadOnlyList<Product> Items, int Total)> FetchAllAsync(int page, int size);
        Task<(IReadOnlyList<Product> Items, int Total)> FetchBaseAsync(int page, int size, long? categoryId = null);
        Task<IReadOnlyList<Product>> FetchAllAsync();
        Task<(IReadOnlyList<Product> Items, int Total)> FetchFeaturedAsync(int page, int size);

        Task<(IReadOnlyList<Product> Items, int Total)> FetchWithSpecAsync(
            int page, int size, ProductCriteriaDto criteria);

        Task<Product?> GetByIdAsync(long id);

        Task DeleteAsync(long id);

        Task<int> HandleAddProductToCartAsync(string email, long productId, ISession session, int quantity, long? variantId = null);
        Task<int> GetDistinctCountByEmailAsync(string email);

        Task<Cart?> GetCartByUserAsync(User user);

        Task HandleRemoveCartDetailAsync(long cartDetailId, ISession session);

        Task HandleUpdateCartBeforeCheckoutAsync(List<CartDetail> cartDetails);

        Task<long> HandlePlaceOrderAsync(
            User user, ISession session,
            string receiverName, string receiverAddress, string receiverPhone, string? note = null);

        // Cập nhật số lượng của một CartDetail thuộc về user, trả về tổng tiền mới của giỏ hàng
        Task<decimal> UpdateCartQuantityAsync(long cartDetailId, int quantity, long userId, ISession session);

        Task<int> UpdateAsync(Product product);

        Task<IReadOnlyList<Category>> GetAllCategoriesAsync();
        Task<Category?> GetCategoryByIdAsync(long id);
        Task<Category> CreateCategoryAsync(Category category);
        Task UpdateCategoryAsync(Category category);
        Task DeleteCategoryAsync(long id);
        Task<IReadOnlyList<ProductVariant>> GetVariantsAsync(long productId);
        Task<ProductVariant> AddVariantAsync(long productId, string variantName, decimal price, int stock);
        Task UpdateVariantAsync(ProductVariant variant);
        Task DeleteVariantAsync(long variantId);
        Task<ProductVariant?> GetVariantByIdAsync(long variantId);

        // Session Cart methods (for anonymous users)
        Task<int> HandleAddProductToCartSessionAsync(long productId, ISession session, int quantity, long? variantId);
        List<CartItemSession> GetCartFromSession(ISession session);
        Task<long> HandlePlaceOrderFromSessionAsync(ISession session, string receiverName, string receiverAddress, string receiverPhone, string? note = null);
        void RemoveCartItemFromSession(ISession session, long productId, long variantId);
    }

    // Class để lưu giỏ hàng trong Session
    public class CartItemSession
    {
        public long ProductId { get; set; }
        public long VariantId { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
