// File: Controllers/Client/HomePageController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using FastFoodShop.Domain.Entities;
using FastFoodShop.Domain.Interfaces;
using FastFoodShop.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace FastFoodShop.Controllers
{
    public class HomePageController : Controller
    {
        private readonly IProductService _productService;
        private readonly IUserService _userService;
        private readonly IPasswordHasher<User> _hasher;
        private readonly IOrderService _orderService;

        public HomePageController(
            IProductService productService,
            IUserService userService,
            IPasswordHasher<User> hasher,
            IOrderService orderService)
        {
            _productService = productService;
            _userService = userService;
            _hasher = hasher;
            _orderService = orderService;
        }

        // GET /
        [HttpGet("/")]
        public async Task<IActionResult> Index()
        {
            var (products, total) = await _productService.FetchAsync(0, 10);
            return View("~/Views/Client/Homepage/Show.cshtml", products);
        }

        // GET /register
        [HttpGet("/register")]
        public IActionResult Register()
        {
            return View("~/Views/Client/Auth/Register.cshtml", new RegisterDTO());
        }

        // POST /register
        [HttpPost("/register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([FromForm] RegisterDTO dto)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Client/Auth/Register.cshtml", dto);

            // Check email tồn tại chưa
            var emailExists = await _userService.EmailExistsAsync(dto.Email);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng");
                return View("~/Views/Client/Auth/Register.cshtml", dto);
            }

            // Map DTO -> User
            var user = _userService.RegisterDtoToUser(dto);

            // Ghép họ + tên thành FullName
            user.FullName = $"{dto.LastName} {dto.FirstName}".Trim();

            // Hash password
            user.Password = _hasher.HashPassword(user, user.Password);
            user.RoleId = 2; // USER mặc định

            // Save
            await _userService.SaveAsync(user);

            return Redirect("/login");
        }

        // GET /login
        [HttpGet("/login")]
        public IActionResult Login()
        {
            return View("~/Views/Client/Auth/Login.cshtml");
        }

        [HttpPost("/login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([FromForm] LoginDTO dto)
        {
            var user = await _userService.GetByEmailAsync(dto.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
                return View("~/Views/Client/Auth/Login.cshtml", dto);
            }

            var verified = _hasher.VerifyHashedPassword(user, user.Password, dto.Password);
            if (verified == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
                return View("~/Views/Client/Auth/Login.cshtml", dto);
            }

            // Lấy tổng giỏ hàng (Cart.Sum)
            var cartCount = await _orderService.GetCartCountAsync(user.Id);

            // Tạo claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email ?? user.Id.ToString()),
                new Claim("full_name", user.FullName ?? "Tài khoản"),
                new Claim("avatar", user.Avatar ?? "default.png"),
                new Claim("cart_count", cartCount.ToString()),
                new Claim(ClaimTypes.Role, user.Role?.Name ?? "USER")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });

            // ✅ Check role trực tiếp từ user trong DB
            if (user.Role?.Name == "ADMIN")
            {
                return View("~/Views/Admin/Dashboard/Show.cshtml");
            }

            return Redirect("/");
        }


        // GET /access-deny
        [HttpGet("/access-deny")]
        public IActionResult AccessDeny()
        {
            return View("~/Views/Client/Auth/Deny.cshtml");
        }

        [Authorize]
        // GET /order-history
        [HttpGet("/order-history")]
        public async Task<IActionResult> OrderHistory()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Redirect("/login");

            var userId = long.Parse(userIdClaim);
            var user = new User { Id = userId };

            var orders = await _orderService.FetchOrderByUserAsync(user);

            return View("~/Views/Client/Cart/Order-History.cshtml", orders);
        }

        [HttpPost("/logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/");
        }


    }
}
