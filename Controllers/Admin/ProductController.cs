using FastFoodShop.Domain.Interfaces;
using FastFoodShop.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FastFoodShop.Controllers
{
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
            const int pageSize = 5;
            var result = await _products.FetchAsync(page, pageSize);

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
            return RedirectToAction(nameof(Index));
        }

        // GET /admin/products/update/{id}
        [HttpGet("update")]
        public async Task<IActionResult> Update(long id)
        {
            var pr = await _products.GetByIdAsync(id);
            if (pr is null) return RedirectToAction(nameof(Index), new { error = "not_found" });
            ViewBag.Categories = await _products.GetAllCategoriesAsync();
            ViewBag.Variants = await _products.GetVariantsAsync(id);
            return View("~/Views/Admin/Product/Update.cshtml", pr);
        }

        // POST /admin/products/update
        [HttpPost("update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromForm] Product form,
                                               IFormFile nhatraicayFile)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Admin/Product/Update.cshtml", form);

            var current = await _products.GetByIdAsync(form.Id);
            if (current is null) return RedirectToAction(nameof(Index), new { error = "not_found" });

            // cập nhật field
            current.Name = form.Name;
            current.ShortDesc = form.ShortDesc;
            current.DetailDesc = form.DetailDesc;
            current.CategoryId = form.CategoryId;
            current.IsActive = form.IsActive;

            if (nhatraicayFile is { Length: > 0 })
            {
                var fileName = await _upload.SaveFileAsync(nhatraicayFile, "product");
                current.Image = fileName;
            }

            // cần IProductService.UpdateAsync để lưu thay đổi
            await _products.UpdateAsync(current);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("variant/add")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVariant([FromForm] long productId, [FromForm] string variantName, [FromForm] decimal price)
        {
            await _products.AddVariantAsync(productId, variantName, price);
            return RedirectToAction(nameof(Update), new { id = productId });
        }

        [HttpPost("variant/update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateVariant([FromForm] long id, [FromForm] long productId, [FromForm] string variantName, [FromForm] decimal price, [FromForm] bool isActive)
        {
            await _products.UpdateVariantAsync(new ProductVariant { Id = id, ProductId = productId, VariantName = variantName, Price = price, IsActive = isActive });
            return RedirectToAction(nameof(Update), new { id = productId });
        }

        [HttpPost("variant/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVariant([FromForm] long id, [FromForm] long productId)
        {
            await _products.DeleteVariantAsync(id);
            return RedirectToAction(nameof(Update), new { id = productId });
        }

        // GET /admin/products/delete/{id}
        [HttpGet("delete")]
        public async Task<IActionResult> DeleteConfirm(long id)
        {
            var pr = await _products.GetByIdAsync(id);
            if (pr is null) return RedirectToAction(nameof(Index), new { error = "not_found" });

            return View("~/Views/Admin/Product/Delete.cshtml", pr);
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
            if (pr is null) return RedirectToAction(nameof(Index), new { error = "not_found" });
            ViewBag.Variants = await _products.GetVariantsAsync(id);
            return View("~/Views/Admin/Product/Detail.cshtml", pr);
        }
    }
}
