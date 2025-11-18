using FastFoodShop.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastFoodShop.ViewComponents
{
    public class ReviewStatsViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public ReviewStatsViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var totalReviews = await _context.Reviews.CountAsync();

            double averageRating = 0;
            if (totalReviews > 0)
            {
                averageRating = await _context.Reviews.AverageAsync(r => r.Rating);
            }

            var model = new ReviewStatsViewModel
            {
                TotalReviews = totalReviews,
                AverageRating = Math.Round(averageRating, 1)
            };

            return View(model);
        }
    }

    public class ReviewStatsViewModel
    {
        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }
    }
}