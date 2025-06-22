using Api.Services;
using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly IConfigService _configService;
        private readonly IAuditService _auditService;

        public ConfigController(IConfigService configService, IAuditService auditService)
        {
            _configService = configService;
            _auditService = auditService;
        }

        [HttpGet("client")]
        public async Task<IActionResult> GetClientConfig()
        {
            try
            {
                var config = await _configService.GetClientConfigAsync();
                return Ok(config);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetAllConfigurations()
        {
            try
            {
                var configurations = await _configService.GetAllConfigurationsAsync();
                return Ok(configurations);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateConfigurations([FromBody] Dictionary<string, string> configurations)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _configService.UpdateConfigurationsAsync(configurations);

                await _auditService.LogAsync(
                    userId,
                    "Update Configuration",
                    "Configuration",
                    null,
                    GetClientIPAddress(),
                    Request.Headers["User-Agent"].ToString(),
                    "System configurations updated"
                );

                return Ok(new { message = "Configurations updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private string GetClientIPAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();

            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }
}
