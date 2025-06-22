using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    [Table("Tbl_AuditLog")]
    public class Tbl_AuditLog
    {
        [Key]
        public int Id { get; set; }

        public string? UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [StringLength(100)]
        public string? EntityType { get; set; }

        public string? EntityId { get; set; }

        [StringLength(45)]
        public string? IPAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        public string? Changes { get; set; } // JSON

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(1000)]
        public string? Description { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
    }
}
