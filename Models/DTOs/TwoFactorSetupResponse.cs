namespace Api.Models.DTOs
{
    public class TwoFactorSetupResponse
    {
        public string QrCodeUrl { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }
}
