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
using System.Collections.Generic;

namespace FastFoodShop.Controllers
{
    public class ItemController : Controller
    {
        private readonly IProductService _products;
        private readonly IUserService _userService;
        private readonly AppDbContext _db;
        private readonly IVnPayService _vnPayService;
        private readonly IOrderService _orderService;
        
        public ItemController(IProductService products, IUserService userService, AppDbContext db, IVnPayService vnPayService, IOrderService orderService)
        {
            _products = products;
            _userService = userService;
            _db = db;
            _vnPayService = vnPayService;
            _orderService = orderService;
        }

        // GET /api/products/{id}/all-variants (Debug endpoint - shows all variants including inactive)
        [HttpGet("api/products/{id:long}/all-variants")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllProductVariants([FromRoute] long id)
        {
            Console.WriteLine($"=== GetAllProductVariants API called for product ID: {id} ===");
            
            var product = await _products.GetByIdAsync(id);
            if (product is null) 
            {
                Console.WriteLine($"❌ Product {id} not found");
                return NotFound(new { message = "Sản phẩm không tồn tại" });
            }

            var variants = await _products.GetVariantsAsync(id);
            Console.WriteLine($"📊 Total variants found: {variants.Count}");
            
            // Log details of all variants
            foreach (var v in variants)
            {
                Console.WriteLine($"  - Variant: ID={v.Id}, Name={v.VariantName}, Price={v.Price}, IsActive={v.IsActive}");
            }

            return Json(new
            {
                productName = product.Name,
                totalVariants = variants.Count,
                activeVariants = variants.Count(v => v.IsActive),
                inactiveVariants = variants.Count(v => !v.IsActive),
                allVariants = variants.Select(v => new
                {
                    id = v.Id,
                    variantName = v.VariantName,
                    price = v.Price,
                    isActive = v.IsActive
                })
            });
        }

        // Authentication helper method
        private bool TryGetUser(out long userId, out string email)
        {
            userId = 0;
            email = string.Empty;

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var emailClaim = User.FindFirst(ClaimTypes.Email);

            Console.WriteLine($"=== TryGetUser DEBUG ===");
            Console.WriteLine($"User.Identity exists: {User?.Identity != null}");
            Console.WriteLine($"User.Identity.IsAuthenticated: {User?.Identity?.IsAuthenticated}");
            Console.WriteLine($"Claims count: {User?.Claims?.Count() ?? 0}");
            Console.WriteLine($"All claims:");
            foreach (var claim in User?.Claims ?? new List<Claim>())
            {
                Console.WriteLine($"  - {claim.Type}: {claim.Value}");
            }
            Console.WriteLine($"userIdClaim: {userIdClaim?.Value ?? "null"}");
            Console.WriteLine($"emailClaim: {emailClaim?.Value ?? "null"}");

            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out userId))
            {
                email = emailClaim?.Value ?? string.Empty;
                Console.WriteLine($"SUCCESS: userId={userId}, email={email}");
                return true;
            }

            Console.WriteLine($"FAILED: userIdClaim is null or invalid");
            return false;
        }

        // GET /api/products/{id}/variants (Public endpoint - no auth required)
        [HttpGet("api/products/{id:long}/variants")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductVariants([FromRoute] long id)
        {
            Console.WriteLine($"=== GetProductVariants API called for product ID: {id} ===");
            
            var product = await _products.GetByIdAsync(id);
            if (product is null) 
            {
                Console.WriteLine($"❌ Product {id} not found");
                return NotFound(new { message = "Sản phẩm không tồn tại" });
            }

            var variants = await _products.GetVariantsAsync(id);
            Console.WriteLine($"📊 Total variants found: {variants.Count}");
            
            // Log details of all variants
            foreach (var v in variants)
            {
                Console.WriteLine($"  - Variant: ID={v.Id}, Name={v.VariantName}, Price={v.Price}, IsActive={v.IsActive}");
            }

            // Return all variants (including inactive) and let frontend handle display
            Console.WriteLine($"✅ Returning all variants (including inactive): {variants.Count}");

            var result = new
            {
                productName = product.Name,
                variants = variants.Select(v => new
                {
                    id = v.Id,
                    variantName = v.VariantName,
                    price = v.Price,
                    // Nếu tồn kho = 0 thì coi như không còn hoạt động đối với phía client
                    isActive = v.IsActive && v.Stock > 0,
                    stock = v.Stock
                })
            };
            
            Console.WriteLine($"🔄 Returning {result.variants.Count()} variants to client");
            return Json(result);
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
        [AllowAnonymous]
        [HttpGet("cart/count")]
        public async Task<IActionResult> GetCartCount()
        {
            int count = 0;
            
            if (TryGetUser(out var userId, out _))
            {
                var cart = await _products.GetCartByUserAsync(new User { Id = userId });
                count = cart?.CartDetails?
                    .Select(d => d.ProductId)
                    .Distinct()
                    .Count() ?? 0;
            }
            else
            {
                // Anonymous user - get from session
                count = HttpContext.Session.GetInt32("distinct") ?? 0;
            }
            
            return Json(new { count });
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


        // GET /cart - display cart
        [AllowAnonymous]
        [HttpGet("cart")]
        public async Task<IActionResult> GetCartPage()
        {
            List<CartDetail> cartDetails = new List<CartDetail>();
            double totalPrice = 0;

            if (TryGetUser(out var userId, out _))
            {
                // Logged in user - lấy từ database
                var user = new User { Id = userId };
                var cart = await _products.GetCartByUserAsync(user);
                cartDetails = cart?.CartDetails?.ToList() ?? new List<CartDetail>();
                foreach (var cd in cartDetails) totalPrice += (double)(cd.Price * cd.Quantity);

                var distinct = cartDetails.Count;
                long totalQtyLong = cartDetails.Sum(d => (long)d.Quantity);
                int totalQty = totalQtyLong > int.MaxValue ? int.MaxValue : (int)totalQtyLong;
                HttpContext.Session.SetInt32("distinct", distinct);
                HttpContext.Session.SetInt32("sum", totalQty);
            }
            else
            {
                // Anonymous user - lấy từ Session
                var sessionItems = _products.GetCartFromSession(HttpContext.Session);
                foreach (var item in sessionItems)
                {
                    var variant = await _products.GetVariantByIdAsync(item.VariantId);
                    var product = await _products.GetByIdAsync(item.ProductId);
                    if (variant != null && product != null)
                    {
                        cartDetails.Add(new CartDetail
                        {
                            Id = item.ProductId * 1000 + item.VariantId, // Temporary ID
                            ProductId = item.ProductId,
                            VariantId = item.VariantId,
                            Price = item.Price,
                            Quantity = item.Quantity,
                            Product = product,
                            Variant = variant
                        });
                        totalPrice += (double)(item.Price * item.Quantity);
                    }
                }
            }

            ViewBag.TotalPrice = totalPrice;
            return View("~/Views/Client/Cart/Show.cshtml", cartDetails);
        }

        // POST /delete-cart-product/{id}
        [AllowAnonymous]
        [HttpPost("delete-cart-product/{id:long}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCartDetail([FromRoute] long id)
        {
            if (TryGetUser(out _, out _))
            {
                // Logged in user - xóa từ database
                await _products.HandleRemoveCartDetailAsync(id, HttpContext.Session);
            }
            else
            {
                // Anonymous user - xóa từ Session
                // id format: ProductId * 1000 + VariantId
                long productId = id / 1000;
                long variantId = id % 1000;
                _products.RemoveCartItemFromSession(HttpContext.Session, productId, variantId);
            }
            return RedirectToAction(nameof(GetCartPage));
        }

        // GET /checkout
        [AllowAnonymous]
        [HttpGet("checkout")]
        public async Task<IActionResult> GetCheckOutPage()
        {
            List<CartDetail> cartDetails = new List<CartDetail>();
            double totalPrice = 0;

            if (TryGetUser(out var userId, out _))
            {
                var user = await _userService.GetByIdAsync(userId);
                if (user != null)
                {
                    var cart = await _products.GetCartByUserAsync(user);
                    cartDetails = cart?.CartDetails?.ToList() ?? new List<CartDetail>();
                    foreach (var cd in cartDetails) totalPrice += (double)(cd.Price * cd.Quantity);
                }
            }
            else
            {
                var sessionItems = _products.GetCartFromSession(HttpContext.Session);
                foreach (var item in sessionItems)
                {
                    var variant = await _products.GetVariantByIdAsync(item.VariantId);
                    var product = await _products.GetByIdAsync(item.ProductId);
                    if (variant != null && product != null)
                    {
                        cartDetails.Add(new CartDetail
                        {
                            Id = item.ProductId * 1000 + item.VariantId,
                            ProductId = item.ProductId,
                            VariantId = item.VariantId,
                            Price = item.Price,
                            Quantity = item.Quantity,
                            Product = product,
                            Variant = variant
                        });
                        totalPrice += (double)(item.Price * item.Quantity);
                    }
                }
            }

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
        [AllowAnonymous]
        [HttpPost("place-order")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HandlePlaceOrder(
            [FromForm] string receiverName,
            [FromForm] string receiverAddress,
            [FromForm] string receiverPhone,
            [FromForm] string paymentMethod,
            [FromForm] string? orderNote = null)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ErrorMessage = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return await GetCheckOutPage();
            }

            if (string.IsNullOrWhiteSpace(receiverName) || string.IsNullOrWhiteSpace(receiverAddress) || string.IsNullOrWhiteSpace(receiverPhone))
            {
                ViewBag.ErrorMessage = "Vui lòng điền đầy đủ thông tin người nhận.";
                return await GetCheckOutPage();
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(receiverPhone, "^[0-9]{10,11}$"))
            {
                ViewBag.ErrorMessage = "Số điện thoại không hợp lệ. Vui lòng nhập 10-11 số.";
                return await GetCheckOutPage();
            }

            try
            {
                decimal totalAmount = 0;
                bool isAnonymous = !TryGetUser(out var userId, out _);

                if (!isAnonymous)
                {
                    // Logged in user
                    var user = await _userService.GetByIdAsync(userId);
                    if (user != null)
                    {
                        var cart = await _products.GetCartByUserAsync(user);
                        totalAmount = cart?.CartDetails?.Sum(cd => cd.Price * cd.Quantity) ?? 0;
                    }
                }
                else
                {
                    // Anonymous user
                    var sessionItems = _products.GetCartFromSession(HttpContext.Session);
                    foreach (var item in sessionItems)
                    {
                        totalAmount += item.Price * item.Quantity;
                    }
                }

                if (paymentMethod == "VNPAY")
                {
                    HttpContext.Session.SetString("ReceiverName", receiverName);
                    HttpContext.Session.SetString("ReceiverAddress", receiverAddress);
                    HttpContext.Session.SetString("ReceiverPhone", receiverPhone);
                    HttpContext.Session.SetString("OrderNote", orderNote ?? "");
                    HttpContext.Session.SetString("IsAnonymous", isAnonymous ? "true" : "false");

                    var orderId = DateTime.Now.Ticks.ToString();
                    var orderDescription = isAnonymous ? $"Thanh toán đơn hàng {orderId}" : $"Thanh toán đơn hàng {orderId} - {receiverName}";
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                    var returnUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/payment-return";

                    var paymentUrl = _vnPayService.CreatePaymentUrl(totalAmount, orderId, orderDescription, returnUrl, ipAddress);
                    return Redirect(paymentUrl);
                }
                else
                {
                    long orderId = 0;
                    if (!isAnonymous)
                    {
                        var user = await _userService.GetByIdAsync(userId);
                        if (user != null)
                        {
                            orderId = await _products.HandlePlaceOrderAsync(user, HttpContext.Session, receiverName, receiverAddress, receiverPhone, orderNote);
                        }
                    }
                    else
                    {
                        orderId = await _products.HandlePlaceOrderFromSessionAsync(HttpContext.Session, receiverName, receiverAddress, receiverPhone, orderNote);
                    }

                    if (orderId <= 0)
                    {
                        ViewBag.ErrorMessage = "Không đủ tồn kho cho một hoặc nhiều sản phẩm trong giỏ hàng. Vui lòng kiểm tra lại.";
                        return await GetCheckOutPage();
                    }

                    return RedirectToAction(nameof(Thanks));
                }
            }
            catch (Exception ex)
            {
                // Log chi tiết lỗi ra console để dễ debug trong môi trường dev
                Console.WriteLine("==== ERROR WHEN PLACING ORDER (COD/VNPAY) ====");
                Console.WriteLine(ex.ToString());

                // Lấy thông điệp chi tiết nhất từ InnerException (nếu có)
                var innerMessage = ex.InnerException?.Message ?? ex.Message;

                // Hiển thị thông báo chi tiết hơn cho người dùng trong môi trường phát triển
                ViewBag.ErrorMessage = $"Có lỗi xảy ra khi đặt hàng. Chi tiết: {innerMessage}";
                return await GetCheckOutPage();
            }
        }

        // GET /thanks
        [HttpGet("thanks")]
        public IActionResult Thanks() => View("~/Views/Client/Cart/Thanks.cshtml");

        // GET /payment-return
        [AllowAnonymous]
        [HttpGet("payment-return")]
        public async Task<IActionResult> PaymentReturn()
        {
            try
            {
                var response = _vnPayService.ProcessPaymentResponse(HttpContext.Request.Query);
                if (response.Success)
                {
                    var isAnonymous = HttpContext.Session.GetString("IsAnonymous") == "true";
                    
                    // Get customer info from session (stored before payment)
                    var receiverName = HttpContext.Session.GetString("ReceiverName") ?? "Khách hàng";
                    var receiverAddress = HttpContext.Session.GetString("ReceiverAddress") ?? "Chưa cập nhật";
                    var receiverPhone = HttpContext.Session.GetString("ReceiverPhone") ?? "Chưa cập nhật";
                    var orderNote = HttpContext.Session.GetString("OrderNote");

                    long orderId = 0;
                    if (isAnonymous)
                    {
                        // Anonymous user - đặt hàng từ Session
                        orderId = await _products.HandlePlaceOrderFromSessionAsync(HttpContext.Session, 
                            receiverName, 
                            receiverAddress, 
                            receiverPhone, 
                            orderNote);
                    }
                    else
                    {
                        // Logged in user
                        if (TryGetUser(out var userId, out _))
                        {
                            var user = await _userService.GetByIdAsync(userId);
                            if (user != null)
                            {
                                orderId = await _products.HandlePlaceOrderAsync(user, HttpContext.Session, 
                                    receiverName, 
                                    receiverAddress, 
                                    receiverPhone, 
                                    orderNote);
                            }
                        }
                    }
                    
                    // Cập nhật PaymentStatus = "PAID" khi thanh toán VnPay thành công
                    if (orderId > 0)
                    {
                        await _orderService.UpdatePaymentStatusAsync(orderId, "PAID");
                    }
                    
                    // Clear session data after successful order
                    HttpContext.Session.Remove("ReceiverName");
                    HttpContext.Session.Remove("ReceiverAddress");
                    HttpContext.Session.Remove("ReceiverPhone");
                    HttpContext.Session.Remove("OrderNote");
                    HttpContext.Session.Remove("IsAnonymous");
                    
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
            catch (Exception)
            {
                ViewBag.PaymentMessage = "Có lỗi xảy ra khi xử lý kết quả thanh toán.";
                ViewBag.PaymentSuccess = false;
                return View("~/Views/Client/Cart/PaymentResult.cshtml");
            }
        }

        // POST /add-product-from-view-detail-test (No auth required for testing)
        [HttpPost("add-product-from-view-detail-test")]
        [IgnoreAntiforgeryToken] // Ignore anti-forgery token for testing
        public async Task<IActionResult> HandleAddProductFromViewDetailTest(
            [FromForm] long id,
            [FromForm] int quantity,
            [FromForm] long? variantId)
        {
            // For testing purposes - log the received data
            Console.WriteLine($"TEST: ProductId={id}, Quantity={quantity}, VariantId={variantId}");
            Console.WriteLine($"TEST: User.Identity.IsAuthenticated = {User?.Identity?.IsAuthenticated}");
            Console.WriteLine($"TEST: Request.Headers[Cookie] = {Request.Headers.ContainsKey("Cookie")}");
            
            if (!TryGetUser(out var userId, out var email))
            {
                Console.WriteLine($"TEST: User not authenticated, returning error");
                return Json(new { success = false, message = "Vui lòng đăng nhập để thêm vào giỏ hàng.", testMode = true });
            }

            try
            {
                var user = await _userService.GetByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng.", testMode = true });
                }

                var result = await _products.HandleAddProductToCartAsync(user.Email ?? "", id, HttpContext.Session, quantity, variantId);

                if (result == -1)
                {
                    return Json(new { success = false, message = "Sản phẩm không đủ tồn kho cho số lượng bạn chọn.", testMode = true });
                }

                if (result > 0)
                {
                    return Json(new { success = true, message = "Thêm sản phẩm vào giỏ hàng thành công! (Test Mode)", testMode = true });
                }

                return Json(new { success = false, message = "Không thể thêm sản phẩm vào giỏ hàng. Vui lòng thử lại.", testMode = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}", testMode = true });
            }
        }

        // POST /add-product-from-view-detail
        [HttpPost("add-product-from-view-detail")]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken] // Cho phép gọi từ JS (homepage) không cần token
        public async Task<IActionResult> HandleAddProductFromViewDetail(
            [FromForm] long id,
            [FromForm] int quantity,
            [FromForm] long? variantId)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            try
            {
                int result;
                int distinct;

                // Nếu đã đăng nhập, dùng database cart
                if (TryGetUser(out var userId, out var email))
                {
                    var user = await _userService.GetByIdAsync(userId);
                    if (user != null)
                    {
                        result = await _products.HandleAddProductToCartAsync(user.Email ?? "", id, HttpContext.Session, quantity, variantId);
                        distinct = HttpContext.Session.GetInt32("distinct") ?? 0;
                    }
                    else
                    {
                        // Fallback to session if user not found
                        result = await _products.HandleAddProductToCartSessionAsync(id, HttpContext.Session, quantity, variantId);
                        distinct = HttpContext.Session.GetInt32("distinct") ?? 0;
                    }
                }
                else
                {
                    // Anonymous user - dùng Session
                    result = await _products.HandleAddProductToCartSessionAsync(id, HttpContext.Session, quantity, variantId);
                    distinct = HttpContext.Session.GetInt32("distinct") ?? 0;
                }

                // -1: không đủ tồn kho
                if (result == -1)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Sản phẩm không đủ tồn kho cho số lượng bạn chọn."
                    });
                }

                if (result > 0)
                {
                    return Json(new { success = true, message = "Thêm sản phẩm vào giỏ hàng thành công!", count = distinct });
                }

                return Json(new { success = false, message = "Không thể thêm sản phẩm vào giỏ hàng." });
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
            [FromQuery] List<string>? price = null,
            [FromQuery] long? categoryId = null,
            [FromQuery] string? category = null)
        {
            Console.WriteLine($"=== GetProductPage DEBUG ===");
            Console.WriteLine($"category parameter: {category}");
            Console.WriteLine($"categoryId parameter: {categoryId}");
            
            const int pageSize = 10;

            var categories = await _products.GetAllCategoriesAsync();
            Console.WriteLine($"Total categories found: {categories.Count()}");
            foreach (var cat in categories)
            {
                Console.WriteLine($"  - Category: ID={cat.Id}, Name={cat.Name}");
            }

            if (!categoryId.HasValue && !string.IsNullOrWhiteSpace(category))
            {
                Console.WriteLine($"Processing category filter: {category}");
                string Normalize(string value) => (value ?? "").Trim().ToLowerInvariant();
                var normalizedQuery = Normalize(category);
                Console.WriteLine($"Normalized category: {normalizedQuery}");

                var slugMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["burger"] = "Burger",
                    ["pizza"] = "Pizza",
                    ["chicken"] = "Gà Rán",
                    ["drinks"] = "Đồ Uống",
                    ["salad"] = "Salad",
                    ["donut"] = "Bánh Donut",
                    ["noodle"] = "Mì Ý",
                    ["soup"] = "Canh",
                };

                if (slugMap.TryGetValue(normalizedQuery, out var mappedName))
                {
                    Console.WriteLine($"Mapped {normalizedQuery} to {mappedName}");
                    normalizedQuery = Normalize(mappedName);
                }

                var matched = categories.FirstOrDefault(c => Normalize(c.Name) == normalizedQuery);
                Console.WriteLine($"Matched category: {matched?.Name ?? "null"}");
                if (matched != null) categoryId = matched.Id;
            }

            var (items, total) = await _products.FetchBaseAsync(page, pageSize, categoryId);
            Console.WriteLine($"Products fetched: {items.Count()} items");
            Console.WriteLine($"Total count: {total}");
            Console.WriteLine($"Selected category ID: {categoryId}");
            
            // Log chi tiết các sản phẩm được load
            foreach (var product in items)
            {
                Console.WriteLine($"  - Product: ID={product.Id}, Name={product.Name}, CategoryId={product.CategoryId}");
            }
            
            ViewBag.Categories = categories;
            ViewBag.SelectedCategoryId = categoryId;

            // Load variants for all products
            var allVariants = new List<FastFoodShop.Domain.Entities.ProductVariant>();
            foreach (var product in items)
            {
                var variants = await _products.GetVariantsAsync(product.Id);
                allVariants.AddRange(variants);
            }

            // Calculate totalPages
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            // QueryString for pagination (remove current page parameter)
            var qs = HttpContext.Request.QueryString.HasValue
                ? HttpContext.Request.QueryString.Value ?? ""
                : "";
            if (!string.IsNullOrEmpty(qs))
            {
                // remove ?page= or &page=
                qs = System.Text.RegularExpressions.Regex.Replace(qs, @"([?&])page=\d+", "$1");
            }

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.QueryString = qs;
            ViewBag.Sort = sort;
            ViewBag.Variants = allVariants;

            return View("~/Views/Client/Product/Show.cshtml", items);
        }

        // GET /products/all - Display all products without pagination
        [HttpGet("products/all")]
        public async Task<IActionResult> GetAllProducts()
        {
            var allProducts = await _products.FetchAllAsync();
            ViewBag.Categories = await _products.GetAllCategoriesAsync();

            // Load variants for all products
            var allVariants = new List<FastFoodShop.Domain.Entities.ProductVariant>();
            foreach (var product in allProducts)
            {
                var variants = await _products.GetVariantsAsync(product.Id);
                allVariants.AddRange(variants);
            }

            ViewBag.Variants = allVariants;
            ViewBag.ShowAll = true; // Flag to indicate this is showing all products

            return View("~/Views/Client/Product/Show.cshtml", allProducts);
        }
    }
}
