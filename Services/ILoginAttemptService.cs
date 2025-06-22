using Api.Models;

namespace Api.Services
{
    public interface ILoginAttemptService
    {
        Task RecordSuccessfulAttemptAsync(string userId, string email, string ipAddress, string userAgent);
        Task RecordFailedAttemptAsync(string email, string ipAddress, string userAgent, string failureReason);
        Task<List<Tbl_LoginAttempt>> GetRecentAttemptsAsync(string email);
        Task<LockoutInfo> GetLockoutInfoAsync(string email);
        Task ClearAttemptsAsync(string email);
    }

    public class LockoutInfo
    {
        public bool IsLocked { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public int AttemptsCount { get; set; }
    }
}
