using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FastFoodShop.Domain.Entities
{
    [Table("products")]
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [StringLength(255)]
        [Display(Name = "Tên sản phẩm")]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "Ảnh đại diện")]
        public string? Image { get; set; }

        [Required(ErrorMessage = "Mô tả chi tiết không được để trống")]
        [Column(TypeName = "nvarchar(max)")]
        [Display(Name = "Mô tả chi tiết")]
        public string DetailDesc { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mô tả ngắn không được để trống")]
        [StringLength(1000)]
        [Display(Name = "Mô tả ngắn")]
        public string ShortDesc { get; set; } = string.Empty;

        [Display(Name = "Danh mục")]
        public long? CategoryId { get; set; }
        public Category? Category { get; set; }

        [Display(Name = "Đang hiển thị")]
        public bool IsActive { get; set; } = true;
        [Display(Name = "Đánh dấu nổi bật")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "Ngày tạo")]
        public DateTime? CreatedAt { get; set; }
        [Display(Name = "Ngày cập nhật")]
        public DateTime? UpdatedAt { get; set; }
    }
}
