using FastFoodShop.Domain.Entities;
using FastFoodShop.Domain.Interfaces;
using FastFoodShop.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FastFoodShop.Controllers.Client
{
    /// <summary>
    /// Controller for handling review-related operations
    /// </summary>
    public class ReviewsController : Controller
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        /// <summary>
        /// Displays the main reviews page with all reviews
        /// </summary>
        /// <returns>Reviews view with list of all reviews</returns>
        [Route("reviews")]
        [Route("Reviews/Show")]
        public async Task<IActionResult> Show()
        {
            var reviews = await _reviewService.GetAllReviewsAsync(20);
            return View("~/Views/Client/Review/Show.cshtml", reviews);
        }

        /// <summary>
        /// Simple test page for reviews functionality
        /// </summary>
        /// <returns>Test view</returns>
        [Route("reviews/test")]
        public async Task<IActionResult> Test()
        {
            // Debug: Check total reviews in database
            var totalReviews = await _reviewService.GetTotalReviewsCountAsync();
            ViewBag.TotalReviews = totalReviews;
            
            // Get some sample reviews for debugging
            var sampleReviews = await _reviewService.GetSampleReviewsAsync(5);
            ViewBag.SampleReviews = sampleReviews;
            
            return View("~/Views/Client/Review/Test.cshtml");
        }

        /// <summary>
        /// Submits a new review
        /// </summary>
        /// <param name="model">Review data</param>
        /// <returns>JSON result indicating success or failure</returns>
        [HttpPost]
        [Route("reviews/submit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(ReviewViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, message = string.Join("\n", errors) });
            }

            var success = await _reviewService.SubmitReviewAsync(model, User);
            
            if (success)
            {
                return Json(new { success = true, message = "Cảm ơn bạn đã đánh giá!" });
            }
            else
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi gửi đánh giá. Vui lòng thử lại." });
            }
        }
    }
}