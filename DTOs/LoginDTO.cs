using System.ComponentModel.DataAnnotations;

namespace FastFoodShop.DTOs
{
    public class LoginDTO
    {
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password không được để trống")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
