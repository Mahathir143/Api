namespace Api.Services
{
    public interface IAuditService
    {
        Task LogAsync(string? userId, string action, string? entityType, string? entityId, string? ipAddress, string? userAgent, string? description, object? changes = null);
    }
}
