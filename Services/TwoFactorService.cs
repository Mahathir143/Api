using Api.Models;
using Api.Models.DTOs;
using Microsoft.AspNetCore.Identity;

namespace Api.Services
{
    public class TwoFactorService : ITwoFactorService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public TwoFactorService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public TwoFactorSetupResponse SetupTwoFactor(string userEmail, bool isEnabled)
        {
            var tfa = new TwoFactorAuthenticator();
            var secretKey = GenerateSecretKey();
            var setupInfo = tfa.GenerateSetupCode("SecureAuthApp", userEmail, secretKey, false, 3);

            return new TwoFactorSetupResponse
            {
                QrCodeUrl = setupInfo.ManualEntryKey,
                SecretKey = secretKey,
                IsEnabled = isEnabled
            };
        }

        public bool ValidateTwoFactorCode(string secretKey, string code)
        {
            var tfa = new TwoFactorAuthenticator();
            return tfa.ValidateTwoFactorPIN(secretKey, code);
        }

        public async Task<bool> EnableTwoFactorAsync(string userId, string code)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.TwoFactorSecretKey))
                return false;

            if (!ValidateTwoFactorCode(user.TwoFactorSecretKey, code))
                return false;

            user.TwoFactorEnabled = true;
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> DisableTwoFactorAsync(string userId, string code)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.TwoFactorEnabled)
                return false;

            if (!ValidateTwoFactorCode(user.TwoFactorSecretKey!, code))
                return false;

            user.TwoFactorEnabled = false;
            user.TwoFactorSecretKey = null;
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        private static string GenerateSecretKey()
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            return new string(Enumerable.Repeat(chars, 16)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
