// File: Models/Order.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FastFoodShop.Domain.Entities
{
    [Table("orders")]
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [Range(typeof(decimal), "0.00", "9999999999999999", ErrorMessage = "Tổng giá trị phải >= 0")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [StringLength(255)]
        public string? ReceiverName { get; set; }

        [StringLength(500)]
        public string? ReceiverAddress { get; set; }

        [StringLength(20)]
        public string? ReceiverPhone { get; set; }

        [StringLength(50)]
        public string? Status { get; set; }

        [StringLength(1000)]
        public string? Note { get; set; }

        // Nhiều Order thuộc về 1 User
        [ForeignKey("UserId")]
        public long? UserId { get; set; }
        public User? User { get; set; }

        // 1 Order có nhiều OrderDetail
        public ICollection<OrderDetail>? OrderDetails { get; set; }

        public override string ToString()
        {
            return $"Order [Id={Id}, TotalPrice={TotalPrice}]";
        }
    }
}
