// File: Models/User.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FastFoodShop.Domain.Entities
{
    [Table("users")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(255)] // thêm để khống chế độ dài trong SQL Server
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password không được để trống")]
        [MinLength(2, ErrorMessage = "Password phải có tối thiểu 2 ký tự")]
        [StringLength(255)] // nên giới hạn max length
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Fullname không được để trống")]
        [MinLength(3, ErrorMessage = "Fullname phải có tối thiểu 3 ký tự")]
        [StringLength(255)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(255)]
        public string? Avatar { get; set; }

        // User nhiều -> 1 Role
        [ForeignKey("RoleId")]
        public int RoleId { get; set; } = 2;
        public Role? Role { get; set; }

        // User 1 -> N Orders
        public ICollection<Order>? Orders { get; set; }

        // User 1 -> 1 Cart
        public Cart? Cart { get; set; }

        public override string ToString()
        {
            return $"User [Id={Id}, Email={Email}, Password={Password}, FullName={FullName}, " +
                   $"Address={Address}, Phone={Phone}, Avatar={Avatar}]";
        }
    }
}
