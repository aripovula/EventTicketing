namespace EventTicketing.Api.Services;

public interface IAuditLogger
{
    Task LogAsync(
        string action,
        string entityType,
        int? entityId = null,
        int? userId = null,
        string? userEmail = null,
        string? details = null);
}
