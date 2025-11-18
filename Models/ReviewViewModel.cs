using System.ComponentModel.DataAnnotations;

namespace FastFoodShop.Models
{
    public class ReviewViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn số sao đánh giá")]
        [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá")]
        [StringLength(1000, ErrorMessage = "Nội dung không được vượt quá 1000 ký tự")]
        public string Content { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự")]
        public string? UserName { get; set; }

        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        public string? UserEmail { get; set; }
    }
}