// File: Models/Cart.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FastFoodShop.Domain.Entities
{
    [Table("carts")]
    public class Cart
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        // 👉 Sửa lại: Sum = số lượng sản phẩm trong giỏ
        [Range(0, int.MaxValue)]
        public int Sum { get; set; }

        // 1 Cart <-> 1 User
        [ForeignKey("UserId")]
        public long? UserId { get; set; }
        public User? User { get; set; }

        // 1 Cart -> nhiều CartDetail
        public ICollection<CartDetail>? CartDetails { get; set; }
    }
}
