using FastFoodShop.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace FastFoodShop.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "ADMIN")]
    [Route("admin/statistics")]
    public class StatisticsController : Controller
    {
        private readonly IUserService _users;
        private readonly IOrderService _orders;

        public StatisticsController(IUserService users, IOrderService orders)
        {
            _users = users;
            _orders = orders;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index([FromQuery] int page = 1)
        {
            const int pageSize = 10;
            
            // Gán dữ liệu cho ViewBag để hiển thị trong Razor
            ViewBag.CountUsers = await _users.CountUsersAsync();
            ViewBag.CountProducts = await _users.CountActiveProductsAsync();
            ViewBag.CountTotalProducts = await _users.CountProductsAsync();
            ViewBag.CountOrders = await _users.CountOrdersAsync();
            ViewBag.CountCategories = await _users.CountCategoriesAsync();
            ViewBag.CountReviews = await _users.CountReviewsAsync();
            ViewBag.AverageRating = await _users.GetAverageRatingAsync();
            
            // Thống kê đơn hàng
            ViewBag.TotalRevenue = await _orders.GetTotalRevenueAsync();
            ViewBag.OrdersByStatus = await _orders.GetOrdersByStatusAsync();
            ViewBag.PendingOrders = await _orders.GetOrdersCountByStatusAsync("PENDING");
            ViewBag.ConfirmedOrders = await _orders.GetOrdersCountByStatusAsync("CONFIRMED");
            ViewBag.DeliveredOrders = await _orders.GetOrdersCountByStatusAsync("DELIVERED");
            ViewBag.CancelledOrders = await _orders.GetOrdersCountByStatusAsync("CANCELLED");

            // Lấy danh sách đơn hàng để hiển thị
            var (orders, total) = await _orders.FetchAllAsync(page, pageSize);
            ViewBag.Orders = orders;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.Total = total;

            // Render view: /Views/Admin/Statistics/Show.cshtml
            return View("~/Views/Admin/Statistics/Show.cshtml");
        }

        // API: /admin/statistics/monthly-revenue?year=2025
        [HttpGet("monthly-revenue")]
        public async Task<IActionResult> GetMonthlyRevenue([FromQuery] int? year)
        {
            var y = year ?? DateTime.Now.Year;
            var data = await _orders.GetMonthlyRevenueAsync(y);

            var labels = Enumerable.Range(1, 12).Select(m => $"T{m}").ToArray();

            return Json(new
            {
                year = y,
                labels,
                data
            });
        }

        // GET /admin/statistics/invoice/{id}
        [HttpGet("invoice/{id:long}")]
        public async Task<IActionResult> GenerateInvoice(long id)
        {
            var order = await _orders.GetByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            return View("~/Views/Admin/Statistics/Invoice.cshtml", order);
        }
    }
}

