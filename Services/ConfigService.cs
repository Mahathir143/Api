using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Api.Services
{
    public class ConfigService : IConfigService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;

        public ConfigService(ApplicationDbContext context, IMemoryCache cache, IConfiguration configuration)
        {
            _context = context;
            _cache = cache;
            _configuration = configuration;
        }

        public async Task<Dictionary<string, object>> GetClientConfigAsync()
        {
            const string cacheKey = "client_config";

            if (_cache.TryGetValue(cacheKey, out Dictionary<string, object>? cachedConfig))
            {
                return cachedConfig!;
            }

            var clientConfigs = await _context.Tbl_Configurations
                .Where(c => c.IsClientVisible)
                .ToDictionaryAsync(c => c.Key, c => c.Value);

            var result = new Dictionary<string, object>();

            // Add configurations from database
            foreach (var config in clientConfigs)
            {
                result[ToCamelCase(config.Key)] = ParseConfigValue(config.Value);
            }

            // Add fallback values from appsettings if not in database
            if (!result.ContainsKey("recaptchaSiteKey"))
            {
                result["recaptchaSiteKey"] = _configuration["GoogleRecaptcha:SiteKey"] ?? "";
            }

            if (!result.ContainsKey("sessionTimeout"))
            {
                result["sessionTimeout"] = _configuration.GetValue<int>("SecuritySettings:SessionTimeoutHours", 2) * 3600; // Convert to seconds
            }

            if (!result.ContainsKey("maxLoginAttempts"))
            {
                result["maxLoginAttempts"] = _configuration.GetValue<int>("SecuritySettings:MaxLoginAttempts", 3);
            }

            if (!result.ContainsKey("lockoutDuration"))
            {
                result["lockoutDuration"] = _configuration.GetValue<int>("SecuritySettings:LockoutDurationMinutes", 5) * 60; // Convert to seconds
            }

            if (!result.ContainsKey("applicationName"))
            {
                result["applicationName"] = _configuration["ApplicationSettings:Name"] ?? "SecureAuth PWA";
            }

            if (!result.ContainsKey("companyName"))
            {
                result["companyName"] = _configuration["ApplicationSettings:CompanyName"] ?? "Your Company Name";
            }

            if (!result.ContainsKey("version"))
            {
                result["version"] = _configuration["ApplicationSettings:Version"] ?? "1.0.0";
            }

            var cacheExpiration = TimeSpan.FromMinutes(
                _configuration.GetValue<int>("CacheSettings:ConfigCacheExpirationMinutes", 60));

            _cache.Set(cacheKey, result, cacheExpiration);

            return result;
        }

        public async Task<Dictionary<string, string>> GetAllConfigurationsAsync()
        {
            return await _context.Tbl_Configurations
                .ToDictionaryAsync(c => c.Key, c => c.Value);
        }

        public async Task UpdateConfigurationsAsync(Dictionary<string, string> configurations)
        {
            foreach (var config in configurations)
            {
                var existingConfig = await _context.Tbl_Configurations
                    .FirstOrDefaultAsync(c => c.Key == config.Key);

                if (existingConfig != null)
                {
                    existingConfig.Value = config.Value;
                    existingConfig.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    _context.Tbl_Configurations.Add(new Tbl_Configuration
                    {
                        Key = config.Key,
                        Value = config.Value,
                        IsClientVisible = false,
                        Category = "Custom"
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Clear cache
            _cache.Remove("client_config");
        }

        private string ToCamelCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return char.ToLowerInvariant(input[0]) + input.Substring(1);
        }

        private object ParseConfigValue(string value)
        {
            if (bool.TryParse(value, out bool boolValue))
                return boolValue;

            if (int.TryParse(value, out int intValue))
                return intValue;

            if (decimal.TryParse(value, out decimal decimalValue))
                return decimalValue;

            return value;
        }
    }
}
