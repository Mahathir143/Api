using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        public bool TwoFactorEnabled { get; set; }

        [StringLength(500)]
        public string? TwoFactorSecretKey { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        [StringLength(45)]
        public string? LastLoginIP { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? LastActivityAt { get; set; }

        // Navigation properties
        public virtual ICollection<Tbl_UserRole> UserRoles { get; set; } = new List<Tbl_UserRole>();
        public virtual ICollection<Tbl_LoginAttempt> LoginAttempts { get; set; } = new List<Tbl_LoginAttempt>();
        public virtual ICollection<Tbl_AuditLog> AuditLogs { get; set; } = new List<Tbl_AuditLog>();
    }
}
