using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    [Table("Tbl_LoginAttempt")]
    public class Tbl_LoginAttempt
    {
        [Key]
        public int Id { get; set; }

        public string? UserId { get; set; }

        [Required]
        [StringLength(320)]
        public string Email { get; set; } = string.Empty;

        [StringLength(45)]
        public string? IPAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        public bool IsSuccessful { get; set; }

        [StringLength(500)]
        public string? FailureReason { get; set; }

        public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

        public bool RequiredTwoFactor { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
    }
}
