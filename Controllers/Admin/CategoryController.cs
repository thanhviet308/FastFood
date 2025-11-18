using FastFoodShop.Domain.Interfaces;
using FastFoodShop.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace FastFoodShop.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "ADMIN")]
    [Route("admin/categories")]
    public class CategoryController : Controller
    {
        private readonly IProductService _products;
        public CategoryController(IProductService products) { _products = products; }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var items = await _products.GetAllCategoriesAsync();
            return View("~/Views/Admin/Category/Show.cshtml", items);
        }

        [HttpGet("create")]
        public IActionResult Create()
        {
            return View("~/Views/Admin/Category/Create.cshtml", new Category());
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] Category form)
        {
            if (string.IsNullOrWhiteSpace(form.Name)) return View("~/Views/Admin/Category/Create.cshtml", form);
            await _products.CreateCategoryAsync(form);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("update")]
        public async Task<IActionResult> Update(long id)
        {
            var c = await _products.GetCategoryByIdAsync(id);
            if (c is null) return RedirectToAction(nameof(Index), new { error = "khong_tim_thay" });
            return View("~/Views/Admin/Category/Update.cshtml", c);
        }

        [HttpPost("update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromForm] Category form)
        {
            await _products.UpdateCategoryAsync(form);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("delete")]
        public async Task<IActionResult> DeleteConfirm(long id)
        {
            var c = await _products.GetCategoryByIdAsync(id);
            if (c is null) return RedirectToAction(nameof(Index), new { error = "khong_tim_thay" });
            return View("~/Views/Admin/Category/Delete.cshtml", c);
        }

        [HttpPost("delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] long id)
        {
            await _products.DeleteCategoryAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}