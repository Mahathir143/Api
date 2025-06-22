using Api.Models.DTOs;

namespace Api.Services
{
    public interface ITwoFactorService
    {
        TwoFactorSetupResponse SetupTwoFactor(string userEmail, bool isEnabled);
        bool ValidateTwoFactorCode(string secretKey, string code);
        Task<bool> EnableTwoFactorAsync(string userId, string code);
        Task<bool> DisableTwoFactorAsync(string userId, string code);
    }
}
