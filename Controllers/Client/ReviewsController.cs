using FastFoodShop.Domain.Entities;
using FastFoodShop.Data;
using FastFoodShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FastFoodShop.Controllers.Client
{
    /// <summary>
    /// Controller for handling review-related operations
    /// </summary>
    public class ReviewsController : Controller
    {
        private readonly AppDbContext _context;

        public ReviewsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Displays the main reviews page with all reviews
        /// </summary>
        /// <returns>Reviews view with list of all reviews</returns>
        [Route("reviews")]
        [Route("Reviews/Show")]
        public async Task<IActionResult> Show()
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .Take(20)
                .ToListAsync();

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
            var totalReviews = await _context.Reviews.CountAsync();
            
            ViewBag.TotalReviews = totalReviews;
            
            // Get some sample reviews for debugging
            var sampleReviews = await _context.Reviews
                .Take(5)
                .Select(r => new {
                    r.Id,
                    r.Rating,
                    r.Content,
                    r.UserName
                })
                .ToListAsync();
            
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

            var review = new Review
            {
                Rating = model.Rating,
                Content = model.Content,
                UserName = model.UserName,
                UserEmail = model.UserEmail,
                CreatedAt = DateTime.UtcNow
            };

            if (User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (long.TryParse(userIdClaim, out long userId))
                {
                    review.UserId = userId;
                    
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        review.UserName = string.IsNullOrEmpty(review.UserName) ? user.FullName : review.UserName;
                        review.UserEmail = string.IsNullOrEmpty(review.UserEmail) ? user.Email : review.UserEmail;
                    }
                }
            }

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cảm ơn bạn đã đánh giá!" });
        }
    }
}