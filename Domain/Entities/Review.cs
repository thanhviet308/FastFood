using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FastFoodShop.Domain.Entities
{
    [Table("reviews")]
    public class Review
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [ForeignKey("User")]
        public long? UserId { get; set; }
        public virtual User? User { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;

        [StringLength(100)]
        public string? UserName { get; set; }

        [StringLength(100)]
        public string? UserEmail { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}