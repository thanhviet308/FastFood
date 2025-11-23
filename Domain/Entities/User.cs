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
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [MinLength(2, ErrorMessage = "Mật khẩu phải có tối thiểu 2 ký tự")]
        [StringLength(255)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ và tên không được để trống")]
        [MinLength(3, ErrorMessage = "Họ và tên phải có tối thiểu 3 ký tự")]
        [StringLength(255)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [StringLength(20)]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [StringLength(255)]
        [Display(Name = "Ảnh đại diện")]
        public string? Avatar { get; set; }

        [ForeignKey("RoleId")]
        [Display(Name = "Vai trò")]
        public int RoleId { get; set; } = 2;
        public Role? Role { get; set; }

        public ICollection<Order>? Orders { get; set; }
        public Cart? Cart { get; set; }
    }
}
