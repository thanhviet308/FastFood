using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FastFoodShop.Domain.Entities
{
    [Table("roles")]
    public class Role
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]  // KHÔNG tự tăng
        public int Id { get; set; }   // Chỉ 1 và 2

        [Required(ErrorMessage = "Tên role không được để trống")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        // 1 Role -> N Users
        public ICollection<User>? Users { get; set; }

        public override string ToString()
        {
            return $"Role [Id={Id}, Name={Name}, Description={Description}]";
        }
    }
}
