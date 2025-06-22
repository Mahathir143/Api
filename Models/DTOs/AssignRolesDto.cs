using System.ComponentModel.DataAnnotations;

namespace Api.Models.DTOs
{
    public class AssignRolesDto
    {
        [Required]
        public List<string> RoleNames { get; set; } = new List<string>();
    }
}
