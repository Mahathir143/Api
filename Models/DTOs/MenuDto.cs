using System.ComponentModel.DataAnnotations;

namespace Api.Models.DTOs
{
    public class MenuDto
    {
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

        public List<MenuDto> Children { get; set; } = new List<MenuDto>();
    }
}
