using FastFoodShop.Domain.Interfaces;
using FastFoodShop.Data;
using Microsoft.EntityFrameworkCore;

namespace FastFoodShop.Services
{
    public class CustomUserService : ICustomUserService
    {
        private readonly AppDbContext _db;

        public CustomUserService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<AppUser?> LoadUserByEmailAsync(string email)
        {
            var user = await _db.Users
                                .Include(u => u.Role)
                                .FirstOrDefaultAsync(u => u.Email == email);

            if (user is null) return null;

            return new AppUser
            {
                Email = user.Email,
                PasswordHash = user.Password, // lưu ý: nên hash trong DB
                Role = $"ROLE_{user.Role?.Name ?? "USER"}"
            };
        }
    }
}
