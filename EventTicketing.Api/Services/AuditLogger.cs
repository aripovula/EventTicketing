using EventTicketing.Api.Data;
using EventTicketing.Api.Models;

namespace EventTicketing.Api.Services;

public class AuditLogger(AppDbContext db) : IAuditLogger
{
    public async Task LogAsync(
        string action,
        string entityType,
        int? entityId = null,
        int? userId = null,
        string? userEmail = null,
        string? details = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            UserEmail = userEmail,
            Details = details,
            Timestamp = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }
}
