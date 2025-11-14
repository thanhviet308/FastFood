using FastFoodShop.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace FastFoodShop.Controllers
{
    [Authorize(Roles = "ADMIN")]
    [Route("admin")]
    public class DashboardController : Controller
    {
        private readonly IUserService _users;

        public DashboardController(IUserService users)
        {
            _users = users;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            // Gán dữ liệu cho ViewBag để hiển thị trong Razor
            ViewBag.CountUsers = await _users.CountUsersAsync();
            ViewBag.CountProducts = await _users.CountProductsAsync();
            ViewBag.CountOrders = await _users.CountOrdersAsync();

            // Render view: /Views/Admin/Dashboard/Show.cshtml
            return View("~/Views/Admin/Dashboard/Show.cshtml");
        }
    }
}
