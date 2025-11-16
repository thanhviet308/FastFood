// File: Models/CartDetail.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FastFoodShop.Domain.Entities
{
    [Table("cart_detail")]
    public class CartDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "Số lượng phải >= 1")]
        public long Quantity { get; set; }

        [Required]
        [Range(0.00, 9999999999999999.99)]
        [Column("unit_price", TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        // Nhiều CartDetail -> 1 Cart
        [ForeignKey("CartId")]
        public long? CartId { get; set; }
        public Cart? Cart { get; set; }

        [ForeignKey("ProductId")]
        public long? ProductId { get; set; }
        public Product? Product { get; set; }

        public long? VariantId { get; set; }
        public ProductVariant? Variant { get; set; }

        [StringLength(255)]
        public string? Note { get; set; }
    }
}
