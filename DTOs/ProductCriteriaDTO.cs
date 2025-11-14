// File: DTOs/ProductCriteriaDTO.cs
namespace FastFoodShop.DTOs
{
    public class ProductCriteriaDTO
    {
        // Dùng string? thay cho Optional<String>
        public string? Page { get; set; }

        // Dùng List<string>? thay cho Optional<List<String>>
        public List<string>? Factory { get; set; }

        public List<string>? Target { get; set; }

        public List<string>? Price { get; set; }

        public string? Sort { get; set; }
    }
}
