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
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password không được để trống")]
        [MinLength(2, ErrorMessage = "Password phải có tối thiểu 2 ký tự")]
        [StringLength(255)]
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

        [ForeignKey("RoleId")]
        public int RoleId { get; set; } = 2;
        public Role? Role { get; set; }

        public ICollection<Order>? Orders { get; set; }
        public Cart? Cart { get; set; }
    }
}
