using Api.Models;
using Api.Models.DTOs;
using Microsoft.AspNetCore.Identity;
using System.Text;
using System.Security.Cryptography;

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
            var secretKey = GenerateSecretKey();
            var qrCodeUrl = GenerateQrCodeUrl("SecureAuthApp", userEmail, secretKey);

            return new TwoFactorSetupResponse
            {
                QrCodeUrl = qrCodeUrl,
                SecretKey = secretKey,
                IsEnabled = isEnabled
            };
        }

        public async Task<TwoFactorSetupResponse> SetupTwoFactorAsync(ApplicationUser user)
        {
            var secretKey = GenerateSecretKey();
            user.TwoFactorSecretKey = secretKey;
            await _userManager.UpdateAsync(user);

            var qrCodeUrl = GenerateQrCodeUrl("SecureAuthApp", user.Email!, secretKey);

            return new TwoFactorSetupResponse
            {
                QrCodeUrl = qrCodeUrl,
                SecretKey = secretKey,
                IsEnabled = user.TwoFactorEnabled
            };
        }

        public bool ValidateTwoFactorCode(string secretKey, string code)
        {
            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(code))
                return false;

            var currentTimeStep = GetCurrentTimeStep();

            // Check current time window and adjacent windows (to account for clock skew)
            for (int i = -1; i <= 1; i++)
            {
                var timeStep = currentTimeStep + i;
                var expectedCode = GenerateTotpCode(secretKey, timeStep);
                if (expectedCode == code)
                    return true;
            }

            return false;
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

        private static string GenerateQrCodeUrl(string issuer, string accountName, string secretKey)
        {
            var encodedIssuer = Uri.EscapeDataString(issuer);
            var encodedAccountName = Uri.EscapeDataString(accountName);
            var encodedSecret = Uri.EscapeDataString(secretKey);

            var totpUrl = $"otpauth://totp/{encodedIssuer}:{encodedAccountName}?secret={encodedSecret}&issuer={encodedIssuer}";

            // Generate QR code URL using a QR code service
            var qrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString(totpUrl)}";

            return qrCodeUrl;
        }

        private static long GetCurrentTimeStep()
        {
            var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return unixTimestamp / 30; // 30-second time window
        }

        private static string GenerateTotpCode(string secretKey, long timeStep)
        {
            var secretKeyBytes = Base32Decode(secretKey);
            var timeStepBytes = BitConverter.GetBytes(timeStep);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(timeStepBytes);

            using var hmac = new HMACSHA1(secretKeyBytes);
            var hash = hmac.ComputeHash(timeStepBytes);

            var offset = hash[hash.Length - 1] & 0xf;
            var binaryCode = (hash[offset] & 0x7f) << 24
                           | (hash[offset + 1] & 0xff) << 16
                           | (hash[offset + 2] & 0xff) << 8
                           | (hash[offset + 3] & 0xff);

            var code = binaryCode % 1000000;
            return code.ToString("D6");
        }

        private static byte[] Base32Decode(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentNullException(nameof(input));

            input = input.TrimEnd('=').ToUpperInvariant();
            var outputLength = input.Length * 5 / 8;
            var output = new byte[outputLength];

            var bitIndex = 0;
            var inputIndex = 0;
            var outputBits = 0;
            var outputIndex = 0;

            while (outputIndex < outputLength)
            {
                var byteIndex = GetBase32CharIndex(input[inputIndex]);
                bitIndex += 5;

                if (bitIndex >= 8)
                {
                    output[outputIndex] = (byte)((outputBits << (8 - bitIndex)) | (byteIndex >> (bitIndex - 8)));
                    outputIndex++;
                    bitIndex -= 8;
                }

                outputBits = byteIndex;
                inputIndex++;
            }

            return output;
        }

        private static int GetBase32CharIndex(char c)
        {
            const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var index = base32Chars.IndexOf(c);
            if (index < 0)
                throw new ArgumentException($"Invalid Base32 character: {c}");
            return index;
        }
    }
}