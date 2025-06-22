namespace Api.Services
{
    public interface IConfigService
    {
        Task<Dictionary<string, object>> GetClientConfigAsync();
        Task<Dictionary<string, string>> GetAllConfigurationsAsync();
        Task UpdateConfigurationsAsync(Dictionary<string, string> configurations);
    }
}
