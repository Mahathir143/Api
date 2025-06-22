namespace Api.Services
{
    public interface IRecaptchaService
    {
        Task<bool> VerifyRecaptchaAsync(string token);
    }
}
