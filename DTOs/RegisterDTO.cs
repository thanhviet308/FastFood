using System.ComponentModel.DataAnnotations;

namespace FastFoodShop.DTOs
{
    public class RegisterDTO
    {
        [Required(ErrorMessage = "FirstName không được để trống")]
        [MinLength(3, ErrorMessage = "FirstName phải có tối thiểu 3 ký tự")]
        public string FirstName { get; set; } = string.Empty;

        public string? LastName { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password không được để trống")]
        [MinLength(3, ErrorMessage = "Password phải có tối thiểu 3 ký tự")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "ConfirmPassword không được để trống")]
        [MinLength(3, ErrorMessage = "ConfirmPassword phải có tối thiểu 3 ký tự")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // Thuộc tính chỉ để map sang User
        public string FullName => string.IsNullOrWhiteSpace(LastName)
            ? FirstName
            : FirstName + " " + LastName;
    }
}
