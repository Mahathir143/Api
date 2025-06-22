using Api.Data;
using Api.Models;
using Api.Models.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IRecaptchaService _recaptchaService;
        private readonly ITwoFactorService _twoFactorService;
        private readonly ILoginAttemptService _loginAttemptService;
        private readonly ApplicationDbContext _context;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IConfiguration configuration,
            IRecaptchaService recaptchaService,
            ITwoFactorService twoFactorService,
            ILoginAttemptService loginAttemptService,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _recaptchaService = recaptchaService;
            _twoFactorService = twoFactorService;
            _loginAttemptService = loginAttemptService;
            _context = context;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto, string clientIP, string userAgent)
        {
            // Verify reCAPTCHA if required
            var attempts = await _loginAttemptService.GetRecentAttemptsAsync(loginDto.Email);
            if (attempts.Count >= 2 && !string.IsNullOrEmpty(loginDto.RecaptchaToken))
            {
                var isRecaptchaValid = await _recaptchaService.VerifyRecaptchaAsync(loginDto.RecaptchaToken);
                if (!isRecaptchaValid)
                {
                    throw new UnauthorizedAccessException("Invalid reCAPTCHA verification");
                }
            }

            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Account is deactivated");
            }

            // Update last login info
            user.LastLoginAt = DateTime.UtcNow;
            user.LastLoginIP = clientIP;
            user.LastActivityAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Record successful login attempt
            await _loginAttemptService.RecordSuccessfulAttemptAsync(user.Id, loginDto.Email, clientIP, userAgent);

            // Check if two-factor authentication is enabled
            if (user.TwoFactorEnabled)
            {
                return new AuthResponseDto
                {
                    RequiresTwoFactor = true,
                    UserId = user.Id
                };
            }

            var token = await GenerateJwtTokenAsync(user);
            await CreateUserSessionAsync(user.Id, token, clientIP, userAgent);

            return new AuthResponseDto
            {
                Token = token,
                User = await MapToUserDtoAsync(user)
            };
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto, string clientIP, string userAgent)
        {
            // Verify reCAPTCHA
            var isRecaptchaValid = await _recaptchaService.VerifyRecaptchaAsync(registerDto.RecaptchaToken);
            if (!isRecaptchaValid)
            {
                throw new UnauthorizedAccessException("Invalid reCAPTCHA verification");
            }

            var user = new ApplicationUser
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                EmailConfirmed = true, // Set to false in production and implement email confirmation
                IsActive = true,
                LastLoginIP = clientIP,
                LastActivityAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            // Assign default role
            var defaultRole = _configuration["ApplicationSettings:DefaultRole"] ?? "User";
            await _userManager.AddToRoleAsync(user, defaultRole);

            var token = await GenerateJwtTokenAsync(user);
            await CreateUserSessionAsync(user.Id, token, clientIP, userAgent);

            return new AuthResponseDto
            {
                Token = token,
                User = await MapToUserDtoAsync(user)
            };
        }

        public async Task<AuthResponseDto> VerifyTwoFactorAsync(TwoFactorVerifyDto twoFactorDto)
        {
            var user = await _userManager.FindByIdAsync(twoFactorDto.UserId);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid user");
            }

            var isValidCode = _twoFactorService.ValidateTwoFactorCode(user.TwoFactorSecretKey!, twoFactorDto.Code);
            if (!isValidCode)
            {
                throw new UnauthorizedAccessException("Invalid two-factor authentication code");
            }

            var token = await GenerateJwtTokenAsync(user);
            await CreateUserSessionAsync(user.Id, token, "Unknown", "Unknown"); // IP/UserAgent would need to be passed

            return new AuthResponseDto
            {
                Token = token,
                User = await MapToUserDtoAsync(user)
            };
        }

        public async Task<UserDto> GetCurrentUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found");
            }

            // Update last activity
            user.LastActivityAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return await MapToUserDtoAsync(user);
        }

        public async Task ExtendSessionAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.LastActivityAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }
        }

        public async Task<bool> ValidateSessionAsync(string userId, string sessionToken)
        {
            var session = _context.Tbl_UserSessions
                .FirstOrDefault(s => s.UserId == userId && s.SessionToken == sessionToken && s.IsActive);

            if (session == null || session.ExpiresAt < DateTime.UtcNow)
            {
                return false;
            }

            // Update last activity
            session.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        private async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]!);

            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim("firstName", user.FirstName),
                new Claim("lastName", user.LastName)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(double.Parse(jwtSettings["ExpirationHours"]!)),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private async Task CreateUserSessionAsync(string userId, string token, string clientIP, string userAgent)
        {
            var sessionTimeoutHours = int.Parse(_configuration["SecuritySettings:SessionTimeoutHours"] ?? "2");

            var session = new Tbl_UserSession
            {
                UserId = userId,
                SessionToken = token,
                IPAddress = clientIP,
                UserAgent = userAgent,
                ExpiresAt = DateTime.UtcNow.AddHours(sessionTimeoutHours),
                LastActivityAt = DateTime.UtcNow
            };

            _context.Tbl_UserSessions.Add(session);
            await _context.SaveChangesAsync();
        }

        private async Task<UserDto> MapToUserDtoAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                TwoFactorEnabled = user.TwoFactorEnabled,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                IsActive = user.IsActive,
                Roles = roles.ToList()
            };
        }
    }
}
