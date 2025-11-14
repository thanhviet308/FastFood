using FastFoodShop.Domain.Interfaces;
using FastFoodShop.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace FastFoodShop.Controllers
{
    [Route("")]
    public class ItemController : Controller
    {
        private readonly IProductService _products;
        public ItemController(IProductService products) => _products = products;

        // GET /product/{id}
        [HttpGet("product/{id:long}")]
        public async Task<IActionResult> GetProductPage([FromRoute] long id)
        {
            var pr = await _products.GetByIdAsync(id);
            if (pr is null) return NotFound();
            ViewBag.Id = id;
            ViewBag.Variants = await _products.GetVariantsAsync(id);
            return View("~/Views/Client/Product/Detail.cshtml", pr);
        }

        // ➤ Helper: lấy userId/email từ Claims
        private bool TryGetUser(out long userId, out string? email)
        {
            userId = 0;
            email = User.Identity?.Name; // bạn set Name = email trong Login
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.TryParse(idClaim, out userId);
        }

        // [Authorize]
        // [HttpPost("/api/add-product-to-cart")]
        // [ValidateAntiForgeryToken]
        // public async Task<IActionResult> AddProductToCart([FromBody] CartRequest req)
        // {
        //     if (!TryGetUser(out _, out var email)) return Unauthorized();
        //     await _products.HandleAddProductToCartAsync(email ?? "", req.ProductId, HttpContext.Session, req.Quantity);
        //     var sum = HttpContext.Session.GetInt32("sum") ?? 0;
        //     return Ok(sum);
        // }

        // public class CartRequest { public long ProductId { get; set; } public int Quantity { get; set; } = 1; }


        // GET /cart  → hiển thị giỏ hàng
        [Authorize]
        [HttpGet("cart")]
        public async Task<IActionResult> GetCartPage()
        {
            if (!TryGetUser(out var userId, out _)) return Redirect("/login");

            var user = new User { Id = userId };
            var cart = await _products.GetCartByUserAsync(user);
            var cartDetails = cart?.CartDetails?.ToList() ?? new List<CartDetail>();

            double totalPrice = 0;
            foreach (var cd in cartDetails) totalPrice += (double)(cd.Price * cd.Quantity);

            ViewBag.TotalPrice = totalPrice;
            ViewBag.Cart = cart;

            // ✅ Cập nhật Session cho badge (nhất quán: badge đọc 'distinct')
            var distinct = cartDetails.Count;
            long totalQtyLong = cartDetails.Sum(d => (long)d.Quantity);
            int totalQty = totalQtyLong > int.MaxValue ? int.MaxValue : (int)totalQtyLong;

            HttpContext.Session.SetInt32("distinct", distinct);
            HttpContext.Session.SetInt32("sum", totalQty);


            return View("~/Views/Client/Cart/Show.cshtml", cartDetails);
        }

        // POST /delete-cart-product/{id}
        [Authorize]
        [HttpPost("delete-cart-product/{id:long}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCartDetail([FromRoute] long id)
        {
            if (!TryGetUser(out _, out _)) return Redirect("/login");
            await _products.HandleRemoveCartDetailAsync(id, HttpContext.Session);
            return RedirectToAction(nameof(GetCartPage));
        }

        // GET /checkout
        [Authorize]
        [HttpGet("checkout")]
        public async Task<IActionResult> GetCheckOutPage()
        {
            if (!TryGetUser(out var userId, out _)) return Redirect("/login");

            var user = new User { Id = userId };
            var cart = await _products.GetCartByUserAsync(user);
            var cartDetails = cart?.CartDetails?.ToList() ?? new List<CartDetail>();

            double totalPrice = 0;
            foreach (var cd in cartDetails) totalPrice += (double)(cd.Price * cd.Quantity);
            ViewBag.TotalPrice = totalPrice;

            return View("~/Views/Client/Cart/Checkout.cshtml", cartDetails);
        }

        // POST /confirm-checkout
        [Authorize]
        [HttpPost("confirm-checkout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCheckout([FromForm] Cart cart)
        {
            if (!TryGetUser(out _, out _)) return Redirect("/login");
            var details = cart?.CartDetails?.ToList() ?? new List<CartDetail>();
            await _products.HandleUpdateCartBeforeCheckoutAsync(details);
            return RedirectToAction(nameof(GetCheckOutPage));
        }

        // POST /place-order
        [Authorize]
        [HttpPost("place-order")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HandlePlaceOrder(
            [FromForm] string receiverName,
            [FromForm] string receiverAddress,
            [FromForm] string receiverPhone)
        {
            if (!TryGetUser(out var userId, out _)) return Redirect("/login");

            var user = new User { Id = userId };
            await _products.HandlePlaceOrderAsync(user, HttpContext.Session, receiverName, receiverAddress, receiverPhone);
            return RedirectToAction(nameof(Thanks));
        }

        // GET /thanks
        [HttpGet("thanks")]
        public IActionResult Thanks() => View("~/Views/Client/Cart/Thanks.cshtml");

        // POST /add-product-from-view-detail
        [Authorize]
        [HttpPost("add-product-from-view-detail")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HandleAddProductFromViewDetail(
            [FromForm] long id,
            [FromForm] int quantity,
            [FromForm] long? variantId)
        {
            if (!TryGetUser(out _, out var email)) return Redirect("/login");
            await _products.HandleAddProductToCartAsync(email ?? "", id, HttpContext.Session, quantity, variantId);
            return Redirect($"/product/{id}");
        }

        // GET /products?page=1&sort=gia-tang-dan&target=...&factory=...&price=...
        [HttpGet("products")]
        public async Task<IActionResult> GetProductPage(
            [FromQuery] int page = 1,
            [FromQuery] string? sort = null,
            [FromQuery] List<string>? target = null,
            [FromQuery] List<string>? factory = null,
            [FromQuery] List<string>? price = null)
        {
            const int pageSize = 10;

            var (items, total) = await _products.FetchBaseAsync(page, pageSize);

            // Tính totalPages
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            // QueryString để build phân trang (loại bỏ tham số page hiện tại)
            var qs = HttpContext.Request.QueryString.HasValue
                ? HttpContext.Request.QueryString.Value ?? ""
                : "";
            if (!string.IsNullOrEmpty(qs))
            {
                // loại bỏ ?page= hoặc &page=
                qs = System.Text.RegularExpressions.Regex.Replace(qs, @"([?&])page=\d+", "$1");
            }

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.QueryString = qs;
            ViewBag.Sort = sort;

            return View("~/Views/Client/Product/Show.cshtml", items);
        }
    }
}
