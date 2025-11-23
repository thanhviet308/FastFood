using FastFoodShop.Domain.Entities;
using FastFoodShop.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace FastFoodShop.Controllers.Client
{
    /// <summary>
    /// Controller for user account management
    /// </summary>
    [Authorize]
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        /// <summary>
        /// Displays the account management page
        /// </summary>
        [Route("account/manage")]
        public async Task<IActionResult> Manage()
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _accountService.GetUserByIdAsync(userId);
            
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
            var success = await _accountService.UpdateUserAsync(userId, model);
            
            if (success)
            {
                return Json(new { success = true, message = "Cập nhật thông tin thành công!" });
            }
            else
            {
                return Json(new { success = false, message = "Người dùng không tồn tại hoặc có lỗi xảy ra" });
            }
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
            var success = await _accountService.ChangePasswordAsync(userId, currentPassword, newPassword);
            
            if (success)
            {
                return Json(new { success = true, message = "Đổi mật khẩu thành công!" });
            }
            else
            {
                return Json(new { success = false, message = "Mật khẩu hiện tại không đúng hoặc có lỗi xảy ra" });
            }
        }

        /// <summary>
        /// Gets user statistics
        /// </summary>
        [Route("account/statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var statistics = await _accountService.GetUserStatisticsAsync(userId);
            
            return Json(new { totalOrders = statistics.TotalOrders, totalReviews = statistics.TotalReviews });
        }

        /// <summary>
        /// Displays the order history page
        /// </summary>
        [Route("orders/history")]
        public async Task<IActionResult> OrderHistory()
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var orders = await _accountService.GetUserOrdersAsync(userId);

            return View("~/Views/Client/Account/OrderHistory.cshtml", orders);
        }

        /// <summary>
        /// Gets order details for AJAX
        /// </summary>
        [Route("orders/details/{id}")]
        public async Task<IActionResult> OrderDetails(long id)
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var order = await _accountService.GetUserOrderByIdAsync(userId, id);

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
                var avatarUrl = await _accountService.UploadAvatarAsync(userId, avatar);

                if (!string.IsNullOrEmpty(avatarUrl))
                {
                    return Json(new { success = true, message = "Cập nhật ảnh đại diện thành công!", avatarUrl = avatarUrl });
                }
                else
                {
                    return Json(new { success = false, message = "Người dùng không tồn tại hoặc có lỗi xảy ra" });
                }
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