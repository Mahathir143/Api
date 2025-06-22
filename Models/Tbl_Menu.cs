using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    [Table("Tbl_Menu")]
    public class Tbl_Menu
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Icon { get; set; }

        [StringLength(500)]
        public string? Url { get; set; }

        public int? ParentId { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("ParentId")]
        public virtual Tbl_Menu? Parent { get; set; }

        public virtual ICollection<Tbl_Menu> Children { get; set; } = new List<Tbl_Menu>();
        public virtual ICollection<Tbl_RoleMenu> RoleMenus { get; set; } = new List<Tbl_RoleMenu>();
    }
}
