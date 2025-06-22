namespace Api.Models.DTOs
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = new();
        public bool RequiresTwoFactor { get; set; }
        public string UserId { get; set; } = string.Empty;
    }
}
