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
        Task<(IReadOnlyList<Product> Items, int Total)> FetchBaseAsync(int page, int size);

        Task<(IReadOnlyList<Product> Items, int Total)> FetchWithSpecAsync(
            int page, int size, ProductCriteriaDto criteria);

        Task<Product?> GetByIdAsync(long id);

        Task DeleteAsync(long id);

        Task HandleAddProductToCartAsync(string email, long productId, ISession session, int quantity, long? variantId = null);
        Task<int> GetDistinctCountByEmailAsync(string email);

        Task<Cart?> GetCartByUserAsync(User user);

        Task HandleRemoveCartDetailAsync(long cartDetailId, ISession session);

        Task HandleUpdateCartBeforeCheckoutAsync(List<CartDetail> cartDetails);

        Task HandlePlaceOrderAsync(
            User user, ISession session,
            string receiverName, string receiverAddress, string receiverPhone);

        Task UpdateAsync(Product product);

        Task<IReadOnlyList<Category>> GetAllCategoriesAsync();
        Task<Category?> GetCategoryByIdAsync(long id);
        Task<Category> CreateCategoryAsync(Category category);
        Task UpdateCategoryAsync(Category category);
        Task DeleteCategoryAsync(long id);
        Task<IReadOnlyList<ProductVariant>> GetVariantsAsync(long productId);
        Task<ProductVariant> AddVariantAsync(long productId, string variantName, decimal price);
        Task UpdateVariantAsync(ProductVariant variant);
        Task DeleteVariantAsync(long variantId);

    }
}
