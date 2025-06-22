using Newtonsoft.Json;

namespace Api.Services
{
    public class RecaptchaService : IRecaptchaService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public RecaptchaService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<bool> VerifyRecaptchaAsync(string token)
        {
            var secretKey = _configuration["GoogleRecaptcha:SecretKey"];
            var requestUri = $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={token}";

            var response = await _httpClient.PostAsync(requestUri, null);
            var responseString = await response.Content.ReadAsStringAsync();
            var recaptchaResponse = JsonConvert.DeserializeObject<RecaptchaResponse>(responseString);

            return recaptchaResponse?.Success == true;
        }

        private class RecaptchaResponse
        {
            public bool Success { get; set; }
        }
    }
}
