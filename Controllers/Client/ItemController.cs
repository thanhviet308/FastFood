using FastFoodShop.Domain.Interfaces;
using FastFoodShop.Domain.Entities;
using FastFoodShop.Data;
using FastFoodShop.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace FastFoodShop.Controllers
{
    [Route("")]
    public class ItemController : Controller
    {
        private readonly IProductService _products;
        private readonly IUserService _userService;
        private readonly AppDbContext _db;
        private readonly IVnPayService _vnPayService;
        
        public ItemController(IProductService products, IUserService userService, AppDbContext db, IVnPayService vnPayService)
        {
            _products = products;
            _userService = userService;
            _db = db;
            _vnPayService = vnPayService;
        }

        // GET /debug/auth
        [HttpGet("debug/auth")]
        public async Task<IActionResult> DebugAuth()
        {
            Console.WriteLine("=== DEBUG AUTH ENDPOINT CALLED ===");

            // Check authentication status
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            Console.WriteLine($"IsAuthenticated: {isAuthenticated}");

            if (isAuthenticated)
            {
                // Get all claims
                Console.WriteLine("All claims:");
                foreach (var claim in User.Claims)
                {
                    Console.WriteLine($"  {claim.Type}: {claim.Value}");
                }

                // Try to get user info
                if (TryGetUser(out var userId, out var email))
                {
                    Console.WriteLine($"User ID from claims: {userId}");
                    Console.WriteLine($"Email from claims: {email}");

                    // Check if user exists in database
                    var user = await _userService.GetByIdAsync(userId);
                    if (user != null)
                    {
                        Console.WriteLine($"User found in database: ID={user.Id}, Email={user.Email}, FullName={user.FullName}");
                    }
                    else
                    {
                        Console.WriteLine($"User NOT found in database for ID: {userId}");

                        // Try to find by email
                        if (!string.IsNullOrEmpty(email))
                        {
                            var userByEmail = await _userService.GetByEmailAsync(email);
                            if (userByEmail != null)
                            {
                                Console.WriteLine($"User found by email: ID={userByEmail.Id}, Email={userByEmail.Email}, FullName={userByEmail.FullName}");
                            }
                            else
                            {
                                Console.WriteLine($"User NOT found by email: {email}");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Failed to parse user ID from claims");
                }
            }
            else
            {
                Console.WriteLine("User is not authenticated");
            }

            // Return JSON response for debugging
            return Json(new
            {
                isAuthenticated,
                claims = User.Claims.Select(c => new { type = c.Type, value = c.Value }),
                userId = TryGetUser(out var uid, out var eml) ? uid : 0,
                email = eml
            });
        }

        // GET /debug/users
        [HttpGet("debug/users")]
        public async Task<IActionResult> DebugUsers()
        {
            Console.WriteLine("=== DEBUG USERS ENDPOINT CALLED ===");

            // Get first 10 users from database
            var (users, total) = await _userService.GetAllAsync(1, 10);

            Console.WriteLine($"Total users in database: {total}");
            foreach (var user in users)
            {
                Console.WriteLine($"User: ID={user.Id}, Email={user.Email}, FullName={user.FullName}, Role={user.Role?.Name}");
            }

            return Json(new
            {
                totalUsers = total,
                users = users.Select(u => new
                {
                    id = u.Id,
                    email = u.Email,
                    fullName = u.FullName,
                    role = u.Role?.Name
                })
            });
        }

        // GET /debug/variants
        [HttpGet("debug/variants")]
        public async Task<IActionResult> DebugVariants()
        {
            Console.WriteLine("=== DEBUG VARIANTS ENDPOINT CALLED ===");

            // Get all variants from database
            var variants = await _db.ProductVariants.ToListAsync();
            Console.WriteLine($"Total variants found: {variants.Count}");

            foreach (var variant in variants)
            {
                Console.WriteLine($"Variant: ID={variant.Id}, ProductID={variant.ProductId}, VariantName={variant.VariantName}, Price={variant.Price}, Active={variant.IsActive}");
            }

            return Json(new
            {
                totalVariants = variants.Count,
                variants = variants.Select(v => new
                {
                    id = v.Id,
                    productId = v.ProductId,
                    variantName = v.VariantName,
                    price = v.Price,
                    isActive = v.IsActive
                })
            });
        }

        // GET /api/products/{id}/variants
        [HttpGet("api/products/{id:long}/variants")]
        public async Task<IActionResult> GetProductVariants([FromRoute] long id)
        {
            var product = await _products.GetByIdAsync(id);
            if (product is null) return NotFound(new { message = "Sản phẩm không tồn tại" });

            var variants = await _products.GetVariantsAsync(id);
            var activeVariants = variants.Where(v => v.IsActive).ToList();

            return Json(new
            {
                productName = product.Name,
                variants = activeVariants.Select(v => new
                {
                    id = v.Id,
                    variantName = v.VariantName,
                    price = v.Price,
                    isActive = v.IsActive
                })
            });
        }

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

        // GET /cart/count
        [HttpGet("cart/count")]
        public async Task<IActionResult> GetCartCount()
        {
            if (!TryGetUser(out var userId, out _))
            {
                return Json(new { count = 0 });
            }

            var cart = await _products.GetCartByUserAsync(new User { Id = userId });
            var count = cart?.CartDetails?
                .Select(d => d.ProductId)
                .Distinct()
                .Count() ?? 0;
            return Json(new { count });
        }

        // ➤ Helper: lấy userId/email từ Claims
        private bool TryGetUser(out long userId, out string? email)
        {
            userId = 0;
            email = User.Identity?.Name;
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
            if (!TryGetUser(out var userId, out var email)) return Redirect("/login");
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

            var user = await _userService.GetByIdAsync(userId);
            if (user == null) return Redirect("/login");

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
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(GetCartPage));
            }

            if (!TryGetUser(out var userId, out _)) return Redirect("/login");

            var user = await _userService.GetByIdAsync(userId);
            if (user == null) return Redirect("/login");

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
            [FromForm] string receiverPhone,
            [FromForm] string paymentMethod)
        {
            

            // Check ModelState
            if (!ModelState.IsValid)
            {
                
                ViewBag.ErrorMessage = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return await GetCheckOutPage();
            }

            // Validate input
            if (string.IsNullOrWhiteSpace(receiverName) || string.IsNullOrWhiteSpace(receiverAddress) || string.IsNullOrWhiteSpace(receiverPhone))
            {
                
                ViewBag.ErrorMessage = "Vui lòng điền đầy đủ thông tin người nhận.";
                return await GetCheckOutPage();
            }

            // Validate phone number format
            if (!System.Text.RegularExpressions.Regex.IsMatch(receiverPhone, "^[0-9]{10,11}$"))
            {
                
                ViewBag.ErrorMessage = "Số điện thoại không hợp lệ. Vui lòng nhập 10-11 số.";
                return await GetCheckOutPage();
            }

            if (!TryGetUser(out var userId, out _)) return Redirect("/login");

            var user = await _userService.GetByIdAsync(userId);
            if (user == null) return Redirect("/login");

            try
            {
                // Get cart to calculate total amount
                var cart = await _products.GetCartByUserAsync(user);
                var totalAmount = cart?.CartDetails?.Sum(cd => cd.Price * cd.Quantity) ?? 0;

                if (paymentMethod == "VNPAY")
                {
                    // Create VNPAY payment URL
                    var orderId = DateTime.Now.Ticks.ToString();
                    var orderDescription = $"Thanh toán đơn hàng {orderId} - {user.FullName}";
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                    var returnUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/payment-return";

                    var paymentUrl = _vnPayService.CreatePaymentUrl(totalAmount, orderId, orderDescription, returnUrl, ipAddress);
                    
                    
                    return Redirect(paymentUrl);
                }
                else
                {
                    await _products.HandlePlaceOrderAsync(user, HttpContext.Session, receiverName, receiverAddress, receiverPhone);
                    
                    return RedirectToAction(nameof(Thanks));
                }
            }
            catch (Exception ex)
            {
                
                ViewBag.ErrorMessage = "Có lỗi xảy ra khi đặt hàng. Vui lòng thử lại.";
                return await GetCheckOutPage();
            }
        }

        // GET /thanks
        [HttpGet("thanks")]
        public IActionResult Thanks() => View("~/Views/Client/Cart/Thanks.cshtml");

        // GET /payment-return
        [HttpGet("payment-return")]
        public async Task<IActionResult> PaymentReturn()
        {
            try
            {
                var response = _vnPayService.ProcessPaymentResponse(HttpContext.Request.Query);
                if (response.Success)
                {
                    if (!TryGetUser(out var userId, out _)) return Redirect("/login");
                    
                    var user = await _userService.GetByIdAsync(userId);
                    if (user == null) return Redirect("/login");
                    
                    await _products.HandlePlaceOrderAsync(user, HttpContext.Session, 
                        $"Payment Order {response.OrderId}", 
                        "VNPAY Payment", 
                        "VNPAY");
                    
                    ViewBag.PaymentMessage = "Thanh toán thành công! Đơn hàng của bạn đã được xác nhận.";
                    ViewBag.PaymentSuccess = true;
                }
                else
                {
                    ViewBag.PaymentMessage = $"Thanh toán thất bại. Mã lỗi: {response.VnPayResponseCode}";
                    ViewBag.PaymentSuccess = false;
                }
                
                return View("~/Views/Client/Cart/PaymentResult.cshtml");
            }
            catch (Exception ex)
            {
                ViewBag.PaymentMessage = "Có lỗi xảy ra khi xử lý kết quả thanh toán.";
                ViewBag.PaymentSuccess = false;
                return View("~/Views/Client/Cart/PaymentResult.cshtml");
            }
        }

        // POST /add-product-from-view-detail
        [Authorize]
        [HttpPost("add-product-from-view-detail")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HandleAddProductFromViewDetail(
            [FromForm] long id,
            [FromForm] int quantity,
            [FromForm] long? variantId)
        {
            if (!TryGetUser(out var userId, out var email))
                return Json(new { success = false, message = "Vui lòng đăng nhập để thêm vào giỏ hàng." });

            

            // Get user by ID instead of email to avoid email mismatch issues
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
            {
                
                return Json(new { success = false, message = "Không tìm thấy thông tin người dùng." });
            }

            try
            {
                var result = await _products.HandleAddProductToCartAsync(user.Email ?? "", id, HttpContext.Session, quantity, variantId);
                

                if (result > 0)
                {
                    return Json(new { success = true, message = "Thêm sản phẩm vào giỏ hàng thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể thêm sản phẩm vào giỏ hàng. Vui lòng thử lại." });
                }
            }
            catch (Exception ex)
            {
                
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
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

            // Load variants for all products
            var allVariants = new List<FastFoodShop.Domain.Entities.ProductVariant>();
            foreach (var product in items)
            {
                var variants = await _products.GetVariantsAsync(product.Id);
                allVariants.AddRange(variants);
            }

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
            ViewBag.Variants = allVariants;

            return View("~/Views/Client/Product/Show.cshtml", items);
        }
    }
}
