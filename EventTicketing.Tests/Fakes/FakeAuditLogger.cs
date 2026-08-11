using EventTicketing.Api.Services;

namespace EventTicketing.Tests.Fakes;

public class FakeAuditLogger : IAuditLogger
{
    public record LogEntry(
        string Action,
        string EntityType,
        int? EntityId,
        int? UserId,
        string? UserEmail,
        string? Details);

    public List<LogEntry> Entries { get; } = [];

    public Task LogAsync(
        string action,
        string entityType,
        int? entityId = null,
        int? userId = null,
        string? userEmail = null,
        string? details = null)
    {
        Entries.Add(new(action, entityType, entityId, userId, userEmail, details));
        return Task.CompletedTask;
    }
}
