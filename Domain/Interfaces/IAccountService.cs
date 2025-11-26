using FastFoodShop.Domain.Entities;
using FastFoodShop.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace FastFoodShop.Domain.Interfaces
{
    public interface IAccountService
    {
        Task<User?> GetUserByIdAsync(long userId);
        Task<User?> GetUserByClaimsAsync(ClaimsPrincipal user);
        Task<bool> UpdateUserAsync(long userId, UpdateAccountDTO model);
        Task<bool> ChangePasswordAsync(long userId, string currentPassword, string newPassword);
        Task<UserStatistics> GetUserStatisticsAsync(long userId);
        Task<List<Order>> GetUserOrdersAsync(long userId);
        Task<Order?> GetUserOrderByIdAsync(long userId, long orderId);
        Task<string?> UploadAvatarAsync(long userId, IFormFile avatar);
        Task<bool> ConfirmOrderReceivedAsync(long userId, long orderId);
    }

    public class UserStatistics
    {
        public int TotalOrders { get; set; }
        public int TotalReviews { get; set; }
    }
}