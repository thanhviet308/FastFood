// File: Models/OrderDetail.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FastFoodShop.Domain.Entities
{
    [Table("order_detail")]
    public class OrderDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "Số lượng phải >= 1")]
        public long Quantity { get; set; }

        [Required]
        [Range(typeof(decimal), "0.00", "9999999999999999")]
        [Column("unit_price", TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        // Nhiều OrderDetail -> 1 Order
        [ForeignKey("OrderId")]
        public long? OrderId { get; set; }
        public Order? Order { get; set; }

        [ForeignKey("ProductId")]
        public long? ProductId { get; set; }
        public Product? Product { get; set; }

        public long? VariantId { get; set; }
        public ProductVariant? Variant { get; set; }
    }
}
