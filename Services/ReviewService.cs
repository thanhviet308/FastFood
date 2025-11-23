using FastFoodShop.Data;
using FastFoodShop.Domain.Entities;
using FastFoodShop.Domain.Interfaces;
using FastFoodShop.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FastFoodShop.Services
{
    public class ReviewService : IReviewService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ReviewService> _logger;

        public ReviewService(AppDbContext context, ILogger<ReviewService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Review>> GetAllReviewsAsync(int take = 20)
        {
            try
            {
                return await _context.Reviews
                    .Include(r => r.User)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(take)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all reviews");
                return new List<Review>();
            }
        }

        public async Task<int> GetTotalReviewsCountAsync()
        {
            try
            {
                return await _context.Reviews.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total reviews count");
                return 0;
            }
        }

        public async Task<List<dynamic>> GetSampleReviewsAsync(int take = 5)
        {
            try
            {
                return await _context.Reviews
                    .Take(take)
                    .Select(r => new {
                        r.Id,
                        r.Rating,
                        r.Content,
                        r.UserName
                    })
                    .ToListAsync<dynamic>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sample reviews");
                return new List<dynamic>();
            }
        }

        public async Task<bool> SubmitReviewAsync(ReviewViewModel model, ClaimsPrincipal user)
        {
            try
            {
                var review = new Review
                {
                    Rating = model.Rating,
                    Content = model.Content,
                    UserName = model.UserName,
                    UserEmail = model.UserEmail,
                    CreatedAt = DateTime.UtcNow
                };

                // Handle authenticated user logic
                if (user?.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (long.TryParse(userIdClaim, out long userId))
                    {
                        review.UserId = userId;
                        
                        var userEntity = await _context.Users.FindAsync(userId);
                        if (userEntity != null)
                        {
                            review.UserName = string.IsNullOrEmpty(review.UserName) 
                                ? userEntity.FullName : review.UserName;
                            review.UserEmail = string.IsNullOrEmpty(review.UserEmail) 
                                ? userEntity.Email : review.UserEmail;
                        }
                    }
                }

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Review submitted successfully by user: {UserName}", 
                    review.UserName ?? "Anonymous");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting review");
                return false;
            }
        }

        public async Task<Review?> GetReviewByIdAsync(long id)
        {
            try
            {
                return await _context.Reviews.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting review by id: {ReviewId}", id);
                return null;
            }
        }
    }
}