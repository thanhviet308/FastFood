using System.Security.Claims;
using FastFoodShop.Domain.Interfaces;
using FastFoodShop.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FastFoodShop.Controllers
{
    [ApiController]
    [Route("api/cart")]
    public class CartApiController : ControllerBase
    {
        private readonly IProductService _products;
        private readonly ILogger<CartApiController> _log;

        public CartApiController(IProductService products, ILogger<CartApiController> log)
        {
            _products = products;
            _log = log;
        }

        // POST /api/cart/add
        [Authorize] // bật lại sau khi test ok
        //[ValidateAntiForgeryToken] // BẬT lại sau khi confirm token ok
        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] CartRequest req)
        {
            try
            {
                var email = User.Identity?.Name; // bạn set Name = email trong Login
                if (string.IsNullOrEmpty(email)) return Unauthorized("No email in claims");

                await _products.HandleAddProductToCartAsync(email, req.ProductId, HttpContext.Session, req.Quantity, req.VariantId);

                // ✅ Badge muốn hiển thị số mặt hàng khác nhau
                var distinct = HttpContext.Session.GetInt32("distinct") ?? 0;
                return Ok(distinct); // JS sẽ cập nhật #sumCart = distinct
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Add to cart failed. Req={@Req}", req);
                return Problem($"AddProductToCart ERROR: {ex.Message}");
            }
        }

        // GET /api/cart/count  → Lấy số mặt hàng khác nhau trực tiếp từ DB (không phụ thuộc session)
        [Authorize] // hoặc [AllowAnonymous] nếu muốn trả 0 khi chưa đăng nhập
        [HttpGet("count")]
        public async Task<IActionResult> Count()
        {
            var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(idStr, out var userId)) return Ok(0);

            var cart = await _products.GetCartByUserAsync(new User { Id = userId });
            var distinct = cart?.CartDetails?.Count ?? 0;
            return Ok(distinct);
        }
    }

    public sealed class CartRequest
    {
        public long ProductId { get; set; }
        public int Quantity { get; set; } = 1;
        public long? VariantId { get; set; }
    }
}
