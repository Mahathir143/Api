using System.ComponentModel.DataAnnotations;

namespace Api.Models.DTOs
{
    public class EnableTwoFactorDto
    {
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Code { get; set; } = string.Empty;
    }
}
