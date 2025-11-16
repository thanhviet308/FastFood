using FastFoodShop.Domain.Entities;
using FastFoodShop.DTOs;  // dùng RegisterDTO

namespace FastFoodShop.Domain.Interfaces
{
    public interface IUserService
    {
        // Phân trang người dùng
        Task<(IReadOnlyList<User> Items, int Total)> GetAllAsync(int page, int size);

        // Tìm theo email (giữ signature trả List như Java)
        Task<List<User>> GetAllByEmailAsync(string email);

        // CRUD cơ bản
        Task<User> SaveAsync(User user);
        Task<User?> GetByIdAsync(long id);
        Task DeleteAsync(long id);

        // Role
        Task<Role?> GetRoleByNameAsync(string name);
        Task<Role?> GetRoleByIdAsync(int id);

        // Mapping DTO -> User (dùng DTO thật của bạn)
        User RegisterDtoToUser(RegisterDTO dto);

        // Kiểm tra/ lấy theo email
        Task<bool> EmailExistsAsync(string email);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetUserByEmailAsync(string email); // alias dùng ở ProductService

        // Đếm số liệu tổng hợp
        Task<long> CountUsersAsync();
        Task<long> CountProductsAsync();
        Task<long> CountOrdersAsync();
    }
}
