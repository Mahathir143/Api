using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    [Table("Tbl_RoleMenu")]
    public class Tbl_RoleMenu
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string RoleId { get; set; } = string.Empty;

        [Required]
        public int MenuId { get; set; }

        public bool CanView { get; set; } = true;

        public bool CanEdit { get; set; } = false;

        public bool CanDelete { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("RoleId")]
        public virtual ApplicationRole Role { get; set; } = null!;

        [ForeignKey("MenuId")]
        public virtual Tbl_Menu Menu { get; set; } = null!;
    }
}
