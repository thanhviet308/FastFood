using System.ComponentModel.DataAnnotations;

namespace FastFoodShop.DTOs
{
    /// <summary>
    /// DTO dùng cho cập nhật thông tin tài khoản (không chứa mật khẩu)
    /// </summary>
    public class UpdateAccountDTO
    {
        [Required(ErrorMessage = "Họ và tên không được để trống")]
        [MinLength(3, ErrorMessage = "Họ và tên phải có tối thiểu 3 ký tự")]
        [StringLength(255)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }
    }
}


