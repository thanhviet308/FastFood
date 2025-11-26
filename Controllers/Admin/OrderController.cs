using FastFoodShop.Domain.Interfaces;
using FastFoodShop.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace FastFoodShop.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "ADMIN")]
    [Route("admin/orders")]
    public class OrderController : Controller
    {
        private readonly IOrderService _orders;

        public OrderController(IOrderService orders)
        {
            _orders = orders;
        }

        // GET /admin/orders?page=1&size=10
        [HttpGet("")]
        public async Task<IActionResult> Index([FromQuery] int? page, [FromQuery] int? size)
        {
            var p = page.GetValueOrDefault(1);
            var s = size.GetValueOrDefault(10);

            var result = await _orders.FetchAllAsync(p, s);
            
            ViewBag.Orders = result.Items;
            ViewBag.CurrentPage = p;
            ViewBag.Total = result.Total;
            ViewBag.PageSize = s;
            ViewBag.TotalPages = (int)Math.Ceiling(result.Total / (double)s);

            return View("~/Views/Admin/Order/Show.cshtml");
        }

        // GET /admin/orders/{id}
        [HttpGet("{id:long}")]
        public async Task<IActionResult> Details(long id)
        {
            var order = await _orders.GetByIdAsync(id);
            if (order is null) return RedirectToAction(nameof(Index), new { error = "khong_tim_thay" });

            // order đã Include OrderDetails + Product trong service
            ViewBag.Id = id;
            return View(
                "~/Views/Admin/Order/Detail.cshtml",
                order.OrderDetails ?? new List<OrderDetail>()
            );
        }

        // GET /admin/orders/delete/{id}
        [HttpGet("delete/{id:long}")]
        public async Task<IActionResult> DeleteConfirm(long id)
        {
            var order = await _orders.GetByIdAsync(id);
            if (order is null) return RedirectToAction(nameof(Index), new { error = "khong_tim_thay" });

            ViewBag.Id = id;
            return View("~/Views/Admin/Order/Delete.cshtml", new Order { Id = id });
        }

        // POST /admin/orders/delete
        [HttpPost("delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] Order form)
        {
            await _orders.DeleteByIdAsync(form.Id);
            return RedirectToAction(nameof(Index));
        }

        // GET /admin/orders/update/{id}
        [HttpGet("update/{id:long}")]
        public async Task<IActionResult> Update(long id)
        {
            var order = await _orders.GetByIdAsync(id);
            if (order is null) return RedirectToAction(nameof(Index));

            return View("~/Views/Admin/Order/Update.cshtml", order);
        }

        // POST /admin/orders/update
        [HttpPost("update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromForm] long id, [FromForm] string status)
        {
            var order = await _orders.GetByIdAsync(id);
            if (order is null)
            {
                return RedirectToAction(nameof(Index));
            }

            // Chỉ cập nhật trạng thái, tránh bind các trường decimal gây lỗi format
            order.Status = status;

            await _orders.UpdateAsync(order);
            return RedirectToAction(nameof(Index));
        }
    }
}
