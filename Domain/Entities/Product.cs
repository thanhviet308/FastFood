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

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Image { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string DetailDesc { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string ShortDesc { get; set; } = string.Empty;

        public long? CategoryId { get; set; }
        public Category? Category { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
