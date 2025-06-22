using System.ComponentModel.DataAnnotations;

namespace Api.Models.DTOs
{
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string RecaptchaToken { get; set; } = string.Empty;
    }
}
