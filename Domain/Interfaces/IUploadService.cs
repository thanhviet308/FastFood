using Microsoft.AspNetCore.Http;

namespace FastFoodShop.Domain.Interfaces
{
    public interface IUploadService
    {
        Task<string> SaveFileAsync(IFormFile file, string targetFolder);
    }
}
