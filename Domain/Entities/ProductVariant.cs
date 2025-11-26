using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FastFoodShop.Domain.Entities
{
    [Table("product_variants")]
    public class ProductVariant
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [ForeignKey("ProductId")]
        public long? ProductId { get; set; }
        public Product? Product { get; set; }

        [Required(ErrorMessage = "Tên biến thể không được để trống")]
        [StringLength(255)]
        [Display(Name = "Tên biến thể")]
        public string VariantName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giá biến thể không được để trống")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá")]
        public decimal Price { get; set; }

        [Display(Name = "Trạng thái biến thể")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Tồn kho")]
        [Range(0, int.MaxValue, ErrorMessage = "Tồn kho phải >= 0")]
        public int Stock { get; set; } = 0;

        [Display(Name = "Ngày tạo")]
        public DateTime? CreatedAt { get; set; }
        [Display(Name = "Ngày cập nhật")]
        public DateTime? UpdatedAt { get; set; }
    }
}