using FastFoodShop.Data;
using FastFoodShop.Domain.Entities;
using FastFoodShop.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FastFoodShop.Services
{
    public class AccountService : IAccountService
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ILogger<AccountService> _logger;

        public AccountService(AppDbContext context, IPasswordHasher<User> passwordHasher, ILogger<AccountService> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task<User?> GetUserByIdAsync(long userId)
        {
            try
            {
                return await _context.Users.FindAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by id: {UserId}", userId);
                return null;
            }
        }

        public async Task<User?> GetUserByClaimsAsync(ClaimsPrincipal user)
        {
            try
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (long.TryParse(userIdClaim, out long userId))
                {
                    return await GetUserByIdAsync(userId);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user from claims");
                return null;
            }
        }

        public async Task<bool> UpdateUserAsync(long userId, User model)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for update: {UserId}", userId);
                    return false;
                }

                // Update user information
                user.FullName = model.FullName;
                user.Email = model.Email;
                user.Phone = model.Phone;
                user.Address = model.Address;

                await _context.SaveChangesAsync();
                _logger.LogInformation("User updated successfully: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(long userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for password change: {UserId}", userId);
                    return false;
                }

                // Verify current password using password hasher (mặc định password đã được hash)
                var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.Password, currentPassword);

                // Nếu verify thất bại, fallback thêm trường hợp password đang lưu là plain text
                if (verifyResult == PasswordVerificationResult.Failed && user.Password != currentPassword)
                {
                    _logger.LogWarning("Invalid current password for user: {UserId}", userId);
                    return false;
                }

                // Hash mật khẩu mới trước khi lưu (kể cả khi trước đó là plain text)
                user.Password = _passwordHasher.HashPassword(user, newPassword);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<UserStatistics> GetUserStatisticsAsync(long userId)
        {
            try
            {
                var totalOrders = await _context.Orders.CountAsync(o => o.UserId == userId);
                var totalReviews = await _context.Reviews.CountAsync(r => r.UserId == userId);

                return new UserStatistics
                {
                    TotalOrders = totalOrders,
                    TotalReviews = totalReviews
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user statistics for user: {UserId}", userId);
                return new UserStatistics { TotalOrders = 0, TotalReviews = 0 };
            }
        }

        public async Task<List<Order>> GetUserOrdersAsync(long userId)
        {
            try
            {
                return await _context.Orders
                    .Include(o => o.OrderDetails)
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.Id)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user orders for user: {UserId}", userId);
                return new List<Order>();
            }
        }

        public async Task<Order?> GetUserOrderByIdAsync(long userId, long orderId)
        {
            try
            {
                return await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.UserId == userId && o.Id == orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user order {OrderId} for user: {UserId}", orderId, userId);
                return null;
            }
        }

        public async Task<string?> UploadAvatarAsync(long userId, IFormFile avatar)
        {
            try
            {
                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
                if (!allowedTypes.Contains(avatar.ContentType))
                {
                    _logger.LogWarning("Invalid file type for avatar upload: {ContentType}", avatar.ContentType);
                    return null;
                }

                // Validate file size (max 5MB)
                if (avatar.Length > 5 * 1024 * 1024)
                {
                    _logger.LogWarning("File too large for avatar upload: {Length} bytes", avatar.Length);
                    return null;
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for avatar upload: {UserId}", userId);
                    return null;
                }

                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Generate unique filename
                var fileName = $"user_{userId}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(avatar.FileName)}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatar.CopyToAsync(stream);
                }

                // Update user avatar URL
                var avatarUrl = $"/uploads/avatars/{fileName}";
                user.Avatar = avatarUrl;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Avatar uploaded successfully for user: {UserId}", userId);
                return avatarUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar for user: {UserId}", userId);
                return null;
            }
        }
    }
}