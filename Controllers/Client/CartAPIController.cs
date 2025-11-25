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
        [AllowAnonymous]
        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] CartRequest req)
        {
            try
            {
                int distinct;
                
                if (User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(User.Identity.Name))
                {
                    // Logged in user
                    await _products.HandleAddProductToCartAsync(User.Identity.Name, req.ProductId, HttpContext.Session, req.Quantity, req.VariantId);
                    distinct = HttpContext.Session.GetInt32("distinct") ?? 0;
                }
                else
                {
                    // Anonymous user
                    distinct = await _products.HandleAddProductToCartSessionAsync(req.ProductId, HttpContext.Session, req.Quantity, req.VariantId);
                }
                
                return Ok(distinct);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Add to cart failed. Req={@Req}", req);
                return Problem($"AddProductToCart ERROR: {ex.Message}");
            }
        }

        // GET /api/cart/count
        [AllowAnonymous]
        [HttpGet("count")]
        public async Task<IActionResult> Count()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (long.TryParse(idStr, out var userId))
                {
                    var cart = await _products.GetCartByUserAsync(new User { Id = userId });
                    var distinct = cart?.CartDetails?.Count ?? 0;
                    return Ok(distinct);
                }
            }
            
            // Anonymous user - lấy từ Session
            var distinctSession = HttpContext.Session.GetInt32("distinct") ?? 0;
            return Ok(distinctSession);
        }

        // POST /api/cart/update-quantity/{cartDetailId}
        [AllowAnonymous]
        [HttpPost("update-quantity/{cartDetailId:long}")]
        public async Task<IActionResult> UpdateQuantity([FromRoute] long cartDetailId, [FromBody] UpdateQuantityRequest body)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!long.TryParse(idStr, out var userId)) return Unauthorized();
                try
                {
                    var newTotal = await _products.UpdateCartQuantityAsync(cartDetailId, body.Quantity, userId, HttpContext.Session);
                    return Ok(new { success = true, newTotal });
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Update quantity failed for cartDetail={CartDetailId}", cartDetailId);
                    return BadRequest(new { success = false, message = ex.Message });
                }
            }
            else
            {
                // Anonymous user - update quantity in session
                // cartDetailId format: ProductId * 1000 + VariantId
                var productId = cartDetailId / 1000;
                var variantId = cartDetailId % 1000;
                var cartItems = _products.GetCartFromSession(HttpContext.Session);
                var item = cartItems.FirstOrDefault(x => x.ProductId == productId && x.VariantId == variantId);
                
                if (item != null)
                {
                    item.Quantity = Math.Clamp(body.Quantity, 1, 999);
                    var updatedJson = System.Text.Json.JsonSerializer.Serialize(cartItems);
                    HttpContext.Session.SetString("AnonymousCart", updatedJson);
                    
                    decimal newTotal = cartItems.Sum(x => x.Price * x.Quantity);
                    return Ok(new { success = true, newTotal });
                }
                
                return BadRequest(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
            }
        }
    }

    public sealed class CartRequest
    {
        public long ProductId { get; set; }
        public int Quantity { get; set; } = 1;
        public long? VariantId { get; set; }
    }

    public sealed class UpdateQuantityRequest
    {
        public int Quantity { get; set; }
    }
}
