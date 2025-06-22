using Api.Data;
using Api.Models;
using Newtonsoft.Json;

namespace Api.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string? userId, string action, string? entityType, string? entityId, string? ipAddress, string? userAgent, string? description, object? changes = null)
        {
            var auditLog = new Tbl_AuditLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                IPAddress = ipAddress,
                UserAgent = userAgent,
                Description = description,
                Changes = changes != null ? JsonConvert.SerializeObject(changes) : null,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tbl_AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
    }
}
