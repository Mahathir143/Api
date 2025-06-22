using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public class LoginAttemptService : ILoginAttemptService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public LoginAttemptService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task RecordSuccessfulAttemptAsync(string userId, string email, string ipAddress, string userAgent)
        {
            var attempt = new Tbl_LoginAttempt
            {
                UserId = userId,
                Email = email,
                IPAddress = ipAddress,
                UserAgent = userAgent,
                IsSuccessful = true,
                AttemptedAt = DateTime.UtcNow
            };

            _context.Tbl_LoginAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            // Clear failed attempts for this email
            await ClearAttemptsAsync(email);
        }

        public async Task RecordFailedAttemptAsync(string email, string ipAddress, string userAgent, string failureReason)
        {
            var attempt = new Tbl_LoginAttempt
            {
                Email = email,
                IPAddress = ipAddress,
                UserAgent = userAgent,
                IsSuccessful = false,
                FailureReason = failureReason,
                AttemptedAt = DateTime.UtcNow
            };

            _context.Tbl_LoginAttempts.Add(attempt);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Tbl_LoginAttempt>> GetRecentAttemptsAsync(string email)
        {
            var lockoutDuration = _configuration.GetValue<int>("SecuritySettings:LockoutDurationMinutes", 5);
            var cutoffTime = DateTime.UtcNow.AddMinutes(-lockoutDuration);

            return await _context.Tbl_LoginAttempts
                .Where(la => la.Email == email && !la.IsSuccessful && la.AttemptedAt > cutoffTime)
                .OrderByDescending(la => la.AttemptedAt)
                .ToListAsync();
        }

        public async Task<LockoutInfo> GetLockoutInfoAsync(string email)
        {
            var maxAttempts = _configuration.GetValue<int>("SecuritySettings:MaxLoginAttempts", 3);
            var lockoutDuration = _configuration.GetValue<int>("SecuritySettings:LockoutDurationMinutes", 5);

            var recentAttempts = await GetRecentAttemptsAsync(email);
            var isLocked = recentAttempts.Count >= maxAttempts;

            DateTime? lockoutEnd = null;
            if (isLocked && recentAttempts.Any())
            {
                var lastAttempt = recentAttempts.First();
                lockoutEnd = lastAttempt.AttemptedAt.AddMinutes(lockoutDuration);

                // Check if lockout period has expired
                if (lockoutEnd <= DateTime.UtcNow)
                {
                    isLocked = false;
                    lockoutEnd = null;
                    await ClearAttemptsAsync(email);
                }
            }

            return new LockoutInfo
            {
                IsLocked = isLocked,
                LockoutEnd = lockoutEnd,
                AttemptsCount = recentAttempts.Count
            };
        }

        public async Task ClearAttemptsAsync(string email)
        {
            var lockoutDuration = _configuration.GetValue<int>("SecuritySettings:LockoutDurationMinutes", 5);
            var cutoffTime = DateTime.UtcNow.AddMinutes(-lockoutDuration);

            var attemptsToRemove = await _context.Tbl_LoginAttempts
                .Where(la => la.Email == email && !la.IsSuccessful && la.AttemptedAt <= cutoffTime)
                .ToListAsync();

            _context.Tbl_LoginAttempts.RemoveRange(attemptsToRemove);
            await _context.SaveChangesAsync();
        }
    }
}
