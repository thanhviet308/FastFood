using FastFoodShop.Domain.Entities;
using FastFoodShop.Models;
using System.Security.Claims;

namespace FastFoodShop.Domain.Interfaces
{
    public interface IReviewService
    {
        Task<List<Review>> GetAllReviewsAsync(int take = 20);
        Task<int> GetTotalReviewsCountAsync();
        Task<List<dynamic>> GetSampleReviewsAsync(int take = 5);
        Task<bool> SubmitReviewAsync(ReviewViewModel model, ClaimsPrincipal user);
        Task<Review?> GetReviewByIdAsync(long id);
    }
}