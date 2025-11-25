using FastFoodShop.Domain.Interfaces;
using FastFoodShop.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
 

namespace FastFoodShop.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "ADMIN")]
    [Route("admin/products")]
    public class ProductController : Controller
    {
        private readonly IUploadService _upload;
        private readonly IProductService _products;

        public ProductController(IUploadService upload, IProductService products)
        {
            _upload = upload;
            _products = products;
        }

        // GET /admin/products?page=1
        [HttpGet("")]
        public async Task<IActionResult> Index([FromQuery] int page = 1)
        {
            const int pageSize = 10;
            // Admin should see ALL products (including inactive) for management
            var result = await _products.FetchAllAsync(page, pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(result.Total / (double)pageSize);

            return View("~/Views/Admin/Product/Show.cshtml", result.Items);
        }

        // GET /admin/products/create
        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            var categories = await _products.GetAllCategoriesAsync();
            ViewBag.Categories = categories;
            return View("~/Views/Admin/Product/Create.cshtml", new Product());
        }

        // POST /admin/products/create
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] Product newProduct,
                                               IFormFile Image)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Admin/Product/Create.cshtml", newProduct);

            if (Image is { Length: > 0 })
            {
                var fileName = await _upload.SaveFileAsync(Image, "product");
                newProduct.Image = fileName;
            }

            await _products.CreateAsync(newProduct);
            return Redirect("/admin/products");
        }

        // GET /admin/products/update/{id}
        [HttpGet("update/{id:long}")]
        public async Task<IActionResult> Update(long id)
        {
            var pr = await _products.GetByIdAsync(id);
            if (pr is null) return RedirectToAction(nameof(Index), new { error = "khong_tim_thay" });
            ViewBag.Categories = await _products.GetAllCategoriesAsync();
            ViewBag.Variants = await _products.GetVariantsAsync(id);
            return View("~/Views/Admin/Product/Update.cshtml", pr);
        }

        // POST /admin/products/update
        // Accept both /admin/products/update and /admin/products/update/{id} because the form
        // may sometimes post back to the current URL which contains the id segment (e.g. /update/2).
        [HttpPost("update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromForm] Product form,
                                                   IFormFile imageFile)
        {
            var current = await _products.GetByIdAsync(form.Id);
            if (current is null) return RedirectToAction(nameof(Index), new { error = "khong_tim_thay" });

            // Remove validation for imageFile since it's optional
            ModelState.Remove("imageFile");

            if (!ModelState.IsValid)
            {
                // Keep current image and populate form data for redisplay
                current.Name = form.Name;
                current.ShortDesc = form.ShortDesc;
                current.DetailDesc = form.DetailDesc;
                current.CategoryId = form.CategoryId;
                current.IsActive = form.IsActive;
                current.IsFeatured = form.IsFeatured;

                ViewBag.Categories = await _products.GetAllCategoriesAsync();
                ViewBag.Variants = await _products.GetVariantsAsync(form.Id);
                return View("~/Views/Admin/Product/Update.cshtml", current);
            }

            // Update fields
            current.Name = form.Name;
            current.ShortDesc = form.ShortDesc;
            current.DetailDesc = form.DetailDesc;
            current.CategoryId = form.CategoryId;
            current.IsActive = form.IsActive;
            current.IsFeatured = form.IsFeatured;
            current.UpdatedAt = DateTime.UtcNow;

            // Handle image upload logic
            if (imageFile != null && imageFile.Length > 0)
            {
                // User selected new image - upload and save
                var fileName = await _upload.SaveFileAsync(imageFile, "product");
                current.Image = fileName;
            }
            else
            {
                // No new image selected - preserve existing image
                // Use the hidden field value from the form, fallback to current database value
                current.Image = form.Image ?? current.Image;
            }

            var affected = await _products.UpdateAsync(current);
            
            if (affected > 0)
            {
                TempData["ProductSuccess"] = "Đã cập nhật sản phẩm";
                return Redirect("/admin/products");
            }
            ModelState.AddModelError(string.Empty, "Không có thay đổi nào được lưu");
            ViewBag.Categories = await _products.GetAllCategoriesAsync();
            ViewBag.Variants = await _products.GetVariantsAsync(form.Id);
            return View("~/Views/Admin/Product/Update.cshtml", current);
        }



        [HttpPost("variant/add")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVariant([FromForm] long productId, [FromForm] string? variantName, [FromForm] decimal? price)
        {
            try
            {
                var name = (variantName ?? string.Empty).Trim();
                var p = price.GetValueOrDefault(0);
                if (string.IsNullOrWhiteSpace(name) || p <= 0)
                {
                    TempData["VariantError"] = "Tên và giá biến thể phải hợp lệ";
                    return RedirectToAction(nameof(Update), new { id = productId });
                }

                await _products.AddVariantAsync(productId, name, p);
                TempData["VariantSuccess"] = "Đã thêm biến thể";
            }
            catch (Exception ex)
            {
                TempData["VariantError"] = "Không thể thêm biến thể: " + ex.Message;
            }
            return RedirectToAction(nameof(Update), new { id = productId });
        }

        [HttpPost("variant/update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateVariant([FromForm] long id, [FromForm] long productId, [FromForm] string variantName, [FromForm] decimal price, [FromForm] bool isActive)
        {
            try
            {
                await _products.UpdateVariantAsync(new ProductVariant { Id = id, ProductId = productId, VariantName = variantName?.Trim() ?? string.Empty, Price = price, IsActive = isActive });
                TempData["VariantSuccess"] = "Đã cập nhật biến thể";
            }
            catch (Exception ex)
            {
                TempData["VariantError"] = "Không thể cập nhật: " + ex.Message;
            }
            return RedirectToAction(nameof(Update), new { id = productId });
        }

        [HttpPost("variant/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVariant([FromForm] long id, [FromForm] long productId)
        {
            try
            {
                await _products.DeleteVariantAsync(id);
                TempData["VariantSuccess"] = "Đã xóa biến thể";
            }
            catch (Exception ex)
            {
                TempData["VariantError"] = "Không thể xóa: " + ex.Message;
            }
            return RedirectToAction(nameof(Update), new { id = productId });
        }

        // GET /admin/products/delete/{id}
        [HttpGet("delete")]
        public async Task<IActionResult> DeleteConfirm(long id)
        {
            var pr = await _products.GetByIdAsync(id);
            if (pr is null) return RedirectToAction(nameof(Index), new { error = "khong_tim_thay" });

            return View("~/Views/Admin/Product/Delete.cshtml", pr);
        }

        // Legacy path support: /admin/Product/Delete?id=123
        [HttpGet("~/admin/Product/Delete")]
        public IActionResult LegacyDeleteRedirect([FromQuery] long id)
        {
            return RedirectToAction(nameof(DeleteConfirm), new { id });
        }


        // POST /admin/products/delete
        [HttpPost("delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] long id)
        {
            await _products.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // GET /admin/products/{id}
        [HttpGet("{id:long}")]
        public async Task<IActionResult> Detail(long id)
        {
            var pr = await _products.GetByIdAsync(id);
            if (pr is null) return RedirectToAction(nameof(Index), new { error = "khong_tim_thay" });
            ViewBag.Variants = await _products.GetVariantsAsync(id);
            ViewBag.CategoryName = pr.Category?.Name;
            return View("~/Views/Admin/Product/Detail.cshtml", pr);
        }
    }
}
