using Api.Models.DTOs;

namespace Api.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto, string clientIP, string userAgent);
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto, string clientIP, string userAgent);
        Task<AuthResponseDto> VerifyTwoFactorAsync(TwoFactorVerifyDto twoFactorDto);
        Task<UserDto> GetCurrentUserAsync(string userId);
        Task ExtendSessionAsync(string userId);
        Task<bool> ValidateSessionAsync(string userId, string sessionToken);
    }
}
