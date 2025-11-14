using FastFoodShop.Domain.Entities;

namespace FastFoodShop.Domain.Interfaces
{
    public interface ICustomUserService
    {
        Task<AppUser?> LoadUserByEmailAsync(string email);
    }

    // DTO đại diện cho user login (tương tự UserDetails trong Spring Security)
    public class AppUser
    {
        public string Email { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public string Role { get; set; } = "USER";   // mặc định
    }
}
