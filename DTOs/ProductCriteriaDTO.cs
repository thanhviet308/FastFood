namespace FastFoodShop.DTOs
{
    public class ProductCriteriaDTO
    {
        public string? Page { get; set; }
        public List<string>? Factory { get; set; }
        public List<string>? Target { get; set; }
        public List<string>? Price { get; set; }
        public string? Sort { get; set; }
    }
}
