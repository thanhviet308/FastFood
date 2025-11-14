using FastFoodShop.Domain.Interfaces;
using FastFoodShop.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FastFoodShop.Controllers
{
    [Route("admin/users")]
    public class UserController : Controller
    {
        private readonly IUserService _users;
        private readonly IUploadService _upload;
        private readonly IPasswordHasher<User> _hasher;

        public UserController(
            IUserService users,
            IUploadService upload,
            IPasswordHasher<User> hasher)
        {
            _users = users;
            _upload = upload;
            _hasher = hasher;
        }

        // GET /admin/users?page=1
        [HttpGet("")]
        public async Task<IActionResult> Index([FromQuery] int page = 1)
        {
            const int pageSize = 5;
            var result = await _users.GetAllAsync(page, pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(result.Total / (double)pageSize);

            // Chỉ rõ path tuyệt đối tới view
            return View("~/Views/Admin/User/Show.cshtml", result.Items);
        }


        // GET /admin/users/{id}
        [HttpGet("{id:long}")]
        public async Task<IActionResult> Detail(long id)
        {
            var u = await _users.GetByIdAsync(id);
            if (u is null) return RedirectToAction(nameof(Index));
            ViewBag.Id = id;
            return View("~/Views/Admin/User/Detail.cshtml", u); // Views/Admin/User/Detail.cshtml
        }

        // GET /admin/users/create
        [HttpGet("create")]
        public IActionResult Create()
    => View("~/Views/Admin/User/Create.cshtml", new User());

        // POST /admin/users/create
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] User form, IFormFile nhatraicayFile)
        {
            // Nếu chưa chọn file thì add lỗi thủ công
            if (nhatraicayFile == null || nhatraicayFile.Length == 0)
            {
                ModelState.AddModelError("Avatar", "Vui lòng chọn ảnh đại diện");
            }

            if (!ModelState.IsValid)
                return View("~/Views/Admin/User/Create.cshtml", form);

            // Upload avatar
            if (nhatraicayFile is { Length: > 0 })
            {
                var avatar = await _upload.SaveFileAsync(nhatraicayFile, "avatar");
                form.Avatar = avatar;
            }

            // Hash password
            form.Password = _hasher.HashPassword(form, form.Password);

            // Map role theo tên (nếu form gửi Role.Name)
            if (!string.IsNullOrWhiteSpace(form.Role?.Name))
            {
                var role = await _users.GetRoleByNameAsync(form.Role.Name);
                if (role != null) form.Role = role;
            }

            await _users.SaveAsync(form);
            return RedirectToAction(nameof(Index));
        }

        // GET /admin/users/update/{id}
        [HttpGet("update")]
        public async Task<IActionResult> Update(long id)
        {
            var current = await _users.GetByIdAsync(id);
            if (current is null) return RedirectToAction(nameof(Index));
            return View("~/Views/Admin/User/Update.cshtml", current);
        }

        // POST /admin/users/update
        [HttpPost("update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromForm] User form)
        {
            var current = await _users.GetByIdAsync(form.Id);
            if (current is null) return RedirectToAction(nameof(Index));

            current.FullName = form.FullName;
            current.Address = form.Address;
            current.Phone = form.Phone;
            // nếu muốn cho đổi role:
            if (!string.IsNullOrWhiteSpace(form.Role?.Name))
            {
                var role = await _users.GetRoleByNameAsync(form.Role.Name);
                if (role != null) current.Role = role;
            }

            await _users.SaveAsync(current);
            return RedirectToAction(nameof(Index));
        }

        // GET /admin/users/delete/{id}
        [HttpGet("delete")]
        public async Task<IActionResult> DeleteConfirm(long id)
        {
            var user = await _users.GetByIdAsync(id);  // nhớ GetByIdAsync phải có Include(u => u.Role)
            if (user == null) return NotFound();

            return View("~/Views/Admin/User/Delete.cshtml", user);
        }

        // POST /admin/users/delete
        [HttpPost("delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] long id)
        {
            await _users.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
