// File: Services/UserService.cs
using FastFoodShop.Domain.Interfaces;
using FastFoodShop.Data;
using FastFoodShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using FastFoodShop.DTOs;   // ✅ dùng RegisterDTO

namespace FastFoodShop.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _db;

        public UserService(AppDbContext db)
        {
            _db = db;
        }

        // --- Phân trang ---
        public async Task<(IReadOnlyList<User> Items, int Total)> GetAllAsync(int page, int size)
        {
            page = Math.Max(1, page);
            size = Math.Clamp(size, 1, 100);

            var q = _db.Users
                .Include(u => u.Role)           // load Role kèm User
                .AsNoTracking()
                .OrderByDescending(u => u.Id);

            var total = await q.CountAsync();

            var items = await q
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return (items, total);
        }

        // Giữ signature List<User> như Java
        public async Task<List<User>> GetAllByEmailAsync(string email)
        {
            return await _db.Users.AsNoTracking()
                                  .Where(u => u.Email == email)
                                  .ToListAsync();
        }

        // --- CRUD ---
        public async Task<User> SaveAsync(User user)
        {
            if (user.Id == 0)
                _db.Users.Add(user);
            else
                _db.Users.Update(user);

            await _db.SaveChangesAsync();
            return user;
        }

        public async Task<User?> GetByIdAsync(long id)
        {
            Console.WriteLine($"GetByIdAsync called for ID: {id}");
            var user = await _db.Users
                .Include(u => u.Role)   // load Role kèm user
                .FirstOrDefaultAsync(u => u.Id == id);
            Console.WriteLine($"GetByIdAsync result: ID={user?.Id}, Email={user?.Email}, FullName={user?.FullName}");
            return user;
        }

        public async Task DeleteAsync(long id)
        {
            var u = await _db.Users.FindAsync(id);
            if (u is null) return;
            _db.Users.Remove(u);
            await _db.SaveChangesAsync();
        }

        // --- Role ---
        public Task<Role?> GetRoleByNameAsync(string name)
            => _db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Name == name);
        
        public Task<Role?> GetRoleByIdAsync(int id)
            => _db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

        // --- Mapping DTO -> User (khớp interface) ---
        public User RegisterDtoToUser(RegisterDTO dto)
        {
            return new User
            {
                FullName = $"{dto.FirstName} {dto.LastName}".Trim(),
                Email = dto.Email,
                // NOTE: ở production nên hash password (IPasswordHasher<User>)
                Password = dto.Password
            };
        }

        // --- Email ---
        public Task<bool> EmailExistsAsync(string email)
            => _db.Users.AnyAsync(u => u.Email == email);

        public async Task<User?> GetByEmailAsync(string email)
        {
            Console.WriteLine($"GetByEmailAsync searching for email: {email}");
            var result = await _db.Users
                .Include(u => u.Role)      // ✅ load luôn Role để login dùng
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);
            Console.WriteLine($"GetByEmailAsync result: {result != null}");
            return result;
        }

        // Alias phục vụ ProductService (tên y hệt)
        public Task<User?> GetUserByEmailAsync(string email) => GetByEmailAsync(email);

        // --- Counters ---
        public Task<long> CountUsersAsync() => _db.Users.LongCountAsync();
        public Task<long> CountProductsAsync() => _db.Products.LongCountAsync();
        public Task<long> CountOrdersAsync() => _db.Orders.LongCountAsync();
    }
}
