using Api.Models;
using Api.Models.DTOs;
using Api.Services;
using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ITwoFactorService _twoFactorService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILoginAttemptService _loginAttemptService;
        private readonly IAuditService _auditService;

        public AuthController(
            IAuthService authService,
            ITwoFactorService twoFactorService,
            UserManager<ApplicationUser> userManager,
            ILoginAttemptService loginAttemptService,
            IAuditService auditService)
        {
            _authService = authService;
            _twoFactorService = twoFactorService;
            _userManager = userManager;
            _loginAttemptService = loginAttemptService;
            _auditService = auditService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var clientIP = GetClientIPAddress();
                var userAgent = Request.Headers["User-Agent"].ToString();

                // Check if account is locked
                var lockoutInfo = await _loginAttemptService.GetLockoutInfoAsync(loginDto.Email);
                if (lockoutInfo.IsLocked)
                {
                    return BadRequest(new
                    {
                        message = $"Account is locked until {lockoutInfo.LockoutEnd:yyyy-MM-dd HH:mm:ss} UTC"
                    });
                }

                var result = await _authService.LoginAsync(loginDto, clientIP, userAgent);

                await _auditService.LogAsync(
                    result.User?.Id,
                    "Login",
                    "User",
                    result.User?.Id,
                    clientIP,
                    userAgent,
                    "Successful login"
                );

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                await _loginAttemptService.RecordFailedAttemptAsync(
                    loginDto.Email,
                    GetClientIPAddress(),
                    Request.Headers["User-Agent"].ToString(),
                    ex.Message
                );

                await _auditService.LogAsync(
                    null,
                    "Login Failed",
                    "User",
                    null,
                    GetClientIPAddress(),
                    Request.Headers["User-Agent"].ToString(),
                    $"Failed login attempt for {loginDto.Email}: {ex.Message}"
                );

                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                var clientIP = GetClientIPAddress();
                var userAgent = Request.Headers["User-Agent"].ToString();

                var result = await _authService.RegisterAsync(registerDto, clientIP, userAgent);

                await _auditService.LogAsync(
                    result.User.Id,
                    "Register",
                    "User",
                    result.User.Id,
                    clientIP,
                    userAgent,
                    "User registered successfully"
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                await _auditService.LogAsync(
                    null,
                    "Register Failed",
                    "User",
                    null,
                    GetClientIPAddress(),
                    Request.Headers["User-Agent"].ToString(),
                    $"Failed registration attempt: {ex.Message}"
                );

                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("verify-2fa")]
        public async Task<IActionResult> VerifyTwoFactor([FromBody] TwoFactorVerifyDto twoFactorDto)
        {
            try
            {
                var result = await _authService.VerifyTwoFactorAsync(twoFactorDto);

                await _auditService.LogAsync(
                    result.User.Id,
                    "2FA Verification",
                    "User",
                    result.User.Id,
                    GetClientIPAddress(),
                    Request.Headers["User-Agent"].ToString(),
                    "Two-factor authentication verified successfully"
                );

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                await _auditService.LogAsync(
                    twoFactorDto.UserId,
                    "2FA Verification Failed",
                    "User",
                    twoFactorDto.UserId,
                    GetClientIPAddress(),
                    Request.Headers["User-Agent"].ToString(),
                    $"Failed 2FA verification: {ex.Message}"
                );

                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var user = await _authService.GetCurrentUserAsync(userId);
                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("extend-session")]
        [Authorize]
        public async Task<IActionResult> ExtendSession()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                await _authService.ExtendSessionAsync(userId);

                await _auditService.LogAsync(
                    userId,
                    "Session Extended",
                    "User",
                    userId,
                    GetClientIPAddress(),
                    Request.Headers["User-Agent"].ToString(),
                    "User session extended"
                );

                return Ok(new { message = "Session extended successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("setup-2fa")]
        [Authorize]
        public async Task<IActionResult> SetupTwoFactor()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var user = await _userManager.FindByIdAsync(userId!);

                if (user == null)
                    return Unauthorized();

                var result = await _twoFactorService.SetupTwoFactorAsync(user);

                await _auditService.LogAsync(
                    userId,
                    "2FA Setup",
                    "User",
                    userId,
                    GetClientIPAddress(),
                    Request.Headers["User-Agent"].ToString(),
                    "Two-factor authentication setup initiated"
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("enable-2fa")]
        [Authorize]
        public async Task<IActionResult> EnableTwoFactor([FromBody] EnableTwoFactorDto enableDto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var result = await _twoFactorService.EnableTwoFactorAsync(userId!, enableDto.Code);

                if (result)
                {
                    await _auditService.LogAsync(
                        userId,
                        "2FA Enabled",
                        "User",
                        userId,
                        GetClientIPAddress(),
                        Request.Headers["User-Agent"].ToString(),
                        "Two-factor authentication enabled"
                    );

                    return Ok(new { message = "Two-factor authentication enabled successfully" });
                }

                return BadRequest(new { message = "Invalid verification code" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("disable-2fa")]
        [Authorize]
        public async Task<IActionResult> DisableTwoFactor([FromBody] EnableTwoFactorDto disableDto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var result = await _twoFactorService.DisableTwoFactorAsync(userId!, disableDto.Code);

                if (result)
                {
                    await _auditService.LogAsync(
                        userId,
                        "2FA Disabled",
                        "User",
                        userId,
                        GetClientIPAddress(),
                        Request.Headers["User-Agent"].ToString(),
                        "Two-factor authentication disabled"
                    );

                    return Ok(new { message = "Two-factor authentication disabled successfully" });
                }

                return BadRequest(new { message = "Invalid verification code" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("login-attempts/{email}")]
        public async Task<IActionResult> GetLoginAttempts(string email)
        {
            try
            {
                var attempts = await _loginAttemptService.GetRecentAttemptsAsync(email);
                var lockoutInfo = await _loginAttemptService.GetLockoutInfoAsync(email);

                return Ok(new
                {
                    attempts = attempts.Count,
                    isLocked = lockoutInfo.IsLocked,
                    lockoutEnd = lockoutInfo.LockoutEnd,
                    recentAttempts = attempts.Take(5)
                });
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

            if (Request.Headers.ContainsKey("X-Real-IP"))
                return Request.Headers["X-Real-IP"].ToString();

            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }
}
