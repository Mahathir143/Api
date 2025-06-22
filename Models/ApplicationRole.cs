using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class ApplicationRole : IdentityRole
    {
        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int SortOrder { get; set; }

        // Navigation properties
        public virtual ICollection<Tbl_UserRole> UserRoles { get; set; } = new List<Tbl_UserRole>();
        public virtual ICollection<Tbl_RoleMenu> RoleMenus { get; set; } = new List<Tbl_RoleMenu>();
    }
}
