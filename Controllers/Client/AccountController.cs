using FastFoodShop.Data;
using FastFoodShop.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FastFoodShop.Controllers.Client
{
    /// <summary>
    /// Controller for user account management
    /// </summary>
    [Authorize]
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Displays the account management page
        /// </summary>
        [Route("account/manage")]
        public async Task<IActionResult> Manage()
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
            {
                return Redirect("/auth/login");
            }

            return View("~/Views/Client/Account/Manage.cshtml", user);
        }

        /// <summary>
        /// Updates user account information
        /// </summary>
        [HttpPost]
        [Route("account/update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(User model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
            }

            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
            {
                return Json(new { success = false, message = "Người dùng không tồn tại" });
            }

            // Update user information
            user.FullName = model.FullName;
            user.Email = model.Email;
            user.Phone = model.Phone;
            user.Address = model.Address;
            // Note: UpdatedAt property doesn't exist in User entity, so we skip this

            await _context.SaveChangesAsync();
            
            return Json(new { success = true, message = "Cập nhật thông tin thành công!" });
        }

        /// <summary>
        /// Changes user password
        /// </summary>
        [HttpPost]
        [Route("account/change-password")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
            {
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ mật khẩu" });
            }

            if (newPassword != confirmPassword)
            {
                return Json(new { success = false, message = "Mật khẩu mới không khớp" });
            }

            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
            {
                return Json(new { success = false, message = "Người dùng không tồn tại" });
            }

            // Verify current password (in a real app, you'd hash passwords)
            if (user.Password != currentPassword)
            {
                return Json(new { success = false, message = "Mật khẩu hiện tại không đúng" });
            }

            user.Password = newPassword;
            // Note: UpdatedAt property doesn't exist in User entity, so we skip this
            
            await _context.SaveChangesAsync();
            
            return Json(new { success = true, message = "Đổi mật khẩu thành công!" });
        }

        /// <summary>
        /// Gets user statistics
        /// </summary>
        [Route("account/statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            var totalOrders = await _context.Orders.CountAsync(o => o.UserId == userId);
            var totalReviews = await _context.Reviews.CountAsync(r => r.UserId == userId);
            
            return Json(new { totalOrders, totalReviews });
        }

        /// <summary>
        /// Displays the order history page
        /// </summary>
        [Route("orders/history")]
        public async Task<IActionResult> OrderHistory()
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.Id) // Use Id as proxy for creation order since CreatedAt doesn't exist
                .ToListAsync();

            return View("~/Views/Client/Account/OrderHistory.cshtml", orders);
        }

        /// <summary>
        /// Gets order details for AJAX
        /// </summary>
        [Route("orders/details/{id}")]
        public async Task<IActionResult> OrderDetails(long id)
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                return Json(new { success = false, message = "Đơn hàng không tồn tại" });
            }

            var orderData = new
            {
                id = order.Id,
                orderNumber = $"ORD-{order.Id:D6}", // Generate order number from Id
                totalAmount = order.TotalPrice,
                status = order.Status,
                createdAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm"), // Use current time since CreatedAt doesn't exist
                receiverName = order.ReceiverName,
                receiverAddress = order.ReceiverAddress,
                receiverPhone = order.ReceiverPhone,
                note = order.Note,
                orderDetails = order.OrderDetails.Select(od => new
                {
                    productName = od.Product?.Name ?? "Sản phẩm không xác định",
                    quantity = od.Quantity,
                    price = od.Price,
                    subtotal = od.Quantity * od.Price
                }).ToList()
            };

            return Json(new { success = true, data = orderData });
        }

        /// <summary>
        /// Uploads and updates user avatar
        /// </summary>
        [HttpPost]
        [Route("account/upload-avatar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile avatar)
        {
            if (avatar == null || avatar.Length == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn ảnh đại diện" });
            }

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
            if (!allowedTypes.Contains(avatar.ContentType))
            {
                return Json(new { success = false, message = "Chỉ chấp nhận file ảnh (JPEG, PNG, GIF)" });
            }

            // Validate file size (max 5MB)
            if (avatar.Length > 5 * 1024 * 1024)
            {
                return Json(new { success = false, message = "Kích thước ảnh không được vượt quá 5MB" });
            }

            try
            {
                var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var user = await _context.Users.FindAsync(userId);
                
                if (user == null)
                {
                    return Json(new { success = false, message = "Người dùng không tồn tại" });
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

                return Json(new { success = true, message = "Cập nhật ảnh đại diện thành công!", avatarUrl = avatarUrl });
            }
            catch (Exception ex)
            {
                // Log error (you might want to add proper logging)
                Console.WriteLine($"Error uploading avatar: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi upload ảnh. Vui lòng thử lại." });
            }
        }
    }
}