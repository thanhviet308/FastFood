using FastFoodShop.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FastFoodShop.Services
{
    public class UploadService : IUploadService
    {
        private readonly IWebHostEnvironment _env;

        public UploadService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string targetFolder)
        {
            if (file == null || file.Length == 0)
                return string.Empty;

            // wwwroot/images/<targetFolder>
            var rootPath = Path.Combine(_env.WebRootPath, "images", targetFolder);
            if (!Directory.Exists(rootPath))
                Directory.CreateDirectory(rootPath);

            var finalName = $"{DateTimeOffset.Now.ToUnixTimeMilliseconds()}-{file.FileName}";
            var filePath = Path.Combine(rootPath, finalName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return finalName;
        }
    }
}
